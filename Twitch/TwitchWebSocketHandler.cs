using System;
using System.Net;
using WebSocketSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using Chat_vs_Streamer;

namespace Chat_vs_Streamer.Twitch
{
    //Ok this will be quite the adventure.
    //I don't know why, but twitlib does not work for this, I have tried a LOT
    //So, after avoiding doing this for a while, I'm just going to do it the old fashioned way.
    //Time to take out the amazing websocket (I am going to kill myself)
    //It's been more than a year since I've done any websocket work, and I used it on Java
    //So, here goes nothing I guess.

    //I am goint to leave a LOT of comments, both for you, if somehow you thought reading the code of an idiot would let you learn
    //But also for me, because I know VERY WELL that I will forget how this works literally tomorrow.


    /// <summary>
    /// Handles the WebSocket connection and communication with Twitch's EventSub API.
    /// This class manages the connection, subscription to events, and processing of incoming messages.
    /// </summary>
    public partial class TwitchWebSocketHandler : MonoBehaviour
    {
        // Twitch API credentials
        private string channelId;      // Twitch channel ID
        private string oauthToken;     // Twitch OAuth token
                                       // TODO: Make an easier way for user to get their token (full backend implementation?)
        private string clientId;       // Twitch app client ID
        private WebSocket ws;          // WebSocket connection instance
        public ChannelPointsRewardsManager rewardsManager; // Channel points rewards manager

        // reward ID, action
        public Dictionary<string, Action> pointsRewardsActions = new Dictionary<string, Action>();

        // bits amount, action
        public Dictionary<string, Action> bitsRewardsActions = new Dictionary<string, Action>();

        // List to track created rewards for cleanup
        private List<string> createdRewardIds = new List<string>();

        /// <summary>
        /// Initializes the Twitch handler with the required credentials
        /// </summary>
        /// <param name="channelId">Twitch channel ID</param>
        /// <param name="oauthToken">Twitch OAuth token</param>
        /// <param name="clientId">Twitch app client ID</param>
        public void Initialize(string channelId, string oauthToken, string clientId)
        {
            this.channelId = channelId;
            this.oauthToken = oauthToken;
            this.clientId = clientId;
            this.rewardsManager = new ChannelPointsRewardsManager(channelId, oauthToken, clientId);
        }

        /// <summary>
        /// Adds an action to be executed when a specific channel points reward is redeemed
        /// </summary>
        /// <param name="rewardId">The ID of the channel points reward</param>
        /// <param name="action">The action to be executed</param>
        public void AddChannelPointsAction(string rewardId, Action action)
        {
            if (!pointsRewardsActions.ContainsKey(rewardId))
            {
                pointsRewardsActions.Add(rewardId, action);
                Debug.Log($"Added channel points action for reward ID: {rewardId}");
            }
            else
            {
                pointsRewardsActions[rewardId] = action;
                Debug.Log($"Updated channel points action for reward ID: {rewardId}");
            }
        }

        /// <summary>
        /// Adds an action to be executed when a specific amount of bits is received
        /// </summary>
        /// <param name="bitsAmount">The amount of bits as a string</param>
        /// <param name="action">The action to be executed</param>
        public void AddBitsAction(string bitsAmount, Action action)
        {
            if (!bitsRewardsActions.ContainsKey(bitsAmount))
            {
                bitsRewardsActions.Add(bitsAmount, action);
                Debug.Log($"Added bits action for amount: {bitsAmount}");
            }
            else
            {
                bitsRewardsActions[bitsAmount] = action;
                Debug.Log($"Updated bits action for amount: {bitsAmount}");
            }
        }

        /// <summary>
        /// Establishes a WebSocket connection to Twitch's EventSub service.
        /// Sets up security protocols and event handlers for the connection.
        /// </summary>
        public void Connect()
        {
            // Configure security protocols to support TLS 1.2 and below (If I don't use this twitch cries)
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            // Bypass SSL certificate validation (Still don't know how to make it work with the certificate)
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            Debug.Log("Connecting to Twitch EventSub...");

            // Initialize WebSocket connection to Twitch's EventSub endpoint
            // https://dev.twitch.tv/docs/eventsub/handling-websocket-events/
            ws = new WebSocket("wss://eventsub.wss.twitch.tv/ws");

            // Set up event handlers for the WebSocket connection
            ws.OnMessage += (sender, e) => HandleMessage(e.Data);      // Handle incoming messages
            ws.OnOpen += (sender, e) => Debug.LogAssertion("Connected to Twitch EventSub!");    // Connection established
            ws.OnClose += (sender, e) => Debug.LogWarning("Disconnected from Twitch EventSub!");  // Connection closed
            ws.OnError += (sender, e) => Debug.LogError($"Error: {e.Message}");    // Handle connection errors

            // Establish the WebSocket connection
            ws.Connect();
        }

        /// <summary>
        /// Processes incoming WebSocket messages from Twitch.
        /// Parses the JSON message and routes it to the appropriate handler based on message type.
        /// </summary>
        /// <param name="message">The raw JSON message received from Twitch</param>
        private void HandleMessage(string message)
        {
            //Twitch sends a message like this one
            /*
            {
            "metadata": {
                "message_id": "<message_id>",
                "message_type": "<message_type>",
                "message_timestamp": "<message_timestamp>"
            },
            "payload": {
                <payload> (Varies depending on the event type, so I prefer to just place payload and comment it on the corresponding case)
            }
            }*/

            try
            {
                // Parse the JSON message
                var json = JObject.Parse(message);
                var metadata = json["metadata"];

                // From the metadata tag, get the message type
                // Twitch can have the next types of messages:
                // session_welcome, session_keepalive, notification
                if (metadata != null)
                {
                    var messageType = metadata["message_type"].ToString();

                    switch (messageType)
                    {
                        case "session_welcome":
                            HandleWelcomeMessage(json);     // Initial connection message
                            break;
                        case "notification":
                            HandleNotificationMessage(json);    // Event notification, used for both bits and channel points
                            break;
                        case "session_keepalive":
                            // Keepalive messages maintain the connection
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing message: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the welcome message received when first connecting to Twitch.
        /// Extracts the session ID and initiates subscription to channel point events.
        /// </summary>
        /// <param name="json">The parsed welcome message JSON</param>
        private void HandleWelcomeMessage(JObject json)
        {
            // Welcome message payload is like this:
            /*
            "payload": {
                "session": {
                    "id": "AQoQILE98gtqShGmLD7AM6yJThAB",
                    "status": "connected",
                    "connected_at": "2023-07-19T14:56:51.616329898Z",
                    "keepalive_timeout_seconds": 10,
                    "reconnect_url": null
                }
            }
            */
            // So the ID is extracted to suscribe to the events 
            var sessionId = json["payload"]["session"]["id"].ToString();
            Debug.Log($"Session ID: {sessionId.Substring(0, 10)}...");

            // Subscribe to channel point redemption events using the session ID
            SubscribeToChannelPoints(sessionId);
            SuscribteToBits(sessionId);

            // TODO suscribe to bits too
        }

        /// <summary>
        /// Subscribes to channel point redemption events using the Twitch API.
        /// Creates a subscription request and sends it to Twitch's EventSub endpoint.
        /// </summary>
        /// <param name="sessionId">The WebSocket session ID received from Twitch</param>
        private void SubscribeToChannelPoints(string sessionId)
        {
            // again: https://dev.twitch.tv/docs/eventsub/handling-websocket-events/
            try
            {
                // Create the subscription request object
                // Types of suscriptions: https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/
                var subscription = new
                {
                    type = "channel.channel_points_custom_reward_redemption.add",  // Event type (In docs)
                    version = "1",                                                // API version (Also in docs)
                    condition = new
                    {
                        broadcaster_user_id = channelId                           // Your channel ID (This is not in dosc, why would it be in the docs man this is the id of the channel)
                    },
                    transport = new
                    {
                        method = "websocket",                                     // Use WebSocket transport
                        session_id = sessionId                                    // Current session ID
                    }
                };

                // Convert the subscription object to JSON
                var json = JsonConvert.SerializeObject(subscription);

                // Send the subscription request to Twitch
                using (var client = new WebClient())
                {
                    // Set required headers for the API request
                    client.Headers.Add("Client-Id", clientId);
                    client.Headers.Add("Authorization", $"Bearer {oauthToken}");
                    client.Headers.Add("Content-Type", "application/json");

                    // Send the subscription request
                    var response = client.UploadString("https://api.twitch.tv/helix/eventsub/subscriptions", "POST", json);
                    Debug.LogAssertion("Successfully subscribed to channel point redemptions!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error subscribing: {ex.Message}");
            }
        }

        private void SuscribteToBits(string sessionId)
        {
            try
            {
                var subscription = new
                {
                    type = "channel.bits.use",
                    version = "1",
                    condition = new
                    {
                        broadcaster_user_id = channelId
                    },
                    transport = new
                    {
                        method = "websocket",
                        session_id = sessionId
                    }
                };

                var json = JsonConvert.SerializeObject(subscription);

                using (var client = new WebClient())
                {
                    client.Headers.Add("Client-Id", clientId);
                    client.Headers.Add("Authorization", $"Bearer {oauthToken}");
                    client.Headers.Add("Content-Type", "application/json");

                    var response = client.UploadString("https://api.twitch.tv/helix/eventsub/subscriptions", "POST", json);
                    Debug.LogAssertion("Successfully subscribed to bits redemptions!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error subscribing: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles incoming channel point redemption notifications.
        /// Extracts the redemption details and triggers the OnChannelPointRedemption event.
        /// </summary>
        /// <param name="json">The parsed notification message JSON</param>
        private void HandleNotificationMessage(JObject json)
        {
            // Extract redemption details from the event data
            var notificationType = json["payload"]["subscription"]["type"].ToString();
            var eventData = json["payload"]["event"];

            switch (notificationType)
            {
                case "channel.channel_points_custom_reward_redemption.add":
                    {
                        ChannelPointsRedeemed(eventData);
                        break;
                    }
                case "channel.bits.use":
                    {
                        BitsRedeemed(eventData);
                        break;
                    }
                default:
                    Debug.LogWarning($"Unknown event type: {notificationType}");
                    break;
            }

        }

        /// <summary>
        /// Gets the number of channel points actions currently registered
        /// </summary>
        public int GetChannelPointsActionsCount()
        {
            return pointsRewardsActions.Count;
        }

        /// <summary>
        /// Gets the number of bits actions currently registered
        /// </summary>
        public int GetBitsActionsCount()
        {
            return bitsRewardsActions.Count;
        }

        private void ChannelPointsRedeemed(JToken eventData)
        {
            var userName = eventData["user_name"].ToString();
            var rewardTitle = eventData["reward"]["title"].ToString();
            var userInput = eventData["user_input"]?.ToString() ?? "No input provided";
            var rewardId = eventData["reward"]["id"].ToString();

            // Log the redemption details
            Debug.Log($"-----Channel Point Redemption!-----");
            Debug.Log($"User: {userName}");
            Debug.Log($"Reward: {rewardTitle}");
            Debug.Log($"Input: {userInput}");
            Debug.Log($"Reward ID: {rewardId}");
            Debug.Log($"Current actions in dictionary: {pointsRewardsActions.Count}");
            Debug.Log("-----------------------------------");
            
            if (pointsRewardsActions.TryGetValue(rewardId, out Action action))
            {
                Debug.Log($"Found action for reward ID {rewardId}, executing...");
                action.runAction();
                
                // Display on-screen message
                string actionDescription = GetActionDescription(action);
                TextOnScreen.ShowMessage($"{userName} {actionDescription}!");
            }
            else
            {
                Debug.LogError($"No action found for reward ID: {rewardId}");
            }
        }

        private void BitsRedeemed(JToken eventData)
        {
            var userName = eventData["user_name"].ToString();
            var amount = eventData["bits"].ToString();
            var message = eventData["message"]["text"]?.ToString() ?? "No message provided";

            Debug.Log($"-----Bits Redemption!-----");
            Debug.Log($"User: {userName}");
            Debug.Log($"Amount: {amount}");
            Debug.Log($"Message: {message}");
            Debug.Log($"Current actions in dictionary: {bitsRewardsActions.Count}");
            Debug.Log("-----------------------------------");

            if (bitsRewardsActions.TryGetValue(amount, out Action action))
            {
                Debug.Log($"Found action for {amount} bits, executing...");
                action.runAction();
                
                // Display on-screen message
                string actionDescription = GetActionDescription(action);
                TextOnScreen.ShowMessage($"{userName} {actionDescription}!");
            }
            else
            {
                Debug.LogError($"No action found for {amount} bits");
            }
        }
        
        // Helper method to get a readable description of the action
        private string GetActionDescription(Action action)
        {
            if (!string.IsNullOrEmpty(action.name))
            {
                return action.name;
            }
            
            // If no custom name is set, return a description based on the action type
            switch (action.actionType)
            {
                case Action.ActionType.GiveItem:
                    return "gave you an item";
                case Action.ActionType.RemoveItem:
                    return "took an item from you";
                case Action.ActionType.AlterStats:
                    return "altered your stats";
                case Action.ActionType.SpawnSomething:
                    return "spawned something nearby";
                case Action.ActionType.Teleport:
                    return "teleported you";
                case Action.ActionType.Others:
                    switch (action.others)
                    {
                        case Action.Others.TurnPlayerIntoAFish:
                            return "turned you into a fish";
                        case Action.Others.GiveRandomItem:
                            return "gave you a random item";
                        case Action.Others.RemoveRandomItemFromInventory:
                            return "removed a random item from your inventory";
                        case Action.Others.DehydratePlayer:
                            return "made you dehydrated";
                        case Action.Others.HydratePlayer:
                            return "gave you water";
                        case Action.Others.StarvePlayer:
                            return "made you starve";
                        case Action.Others.FeedPlayer:
                            return "fed you";
                        case Action.Others.WipeInventory:
                            return "wiped your inventory";
                        default:
                            return "did something to you";
                    }
                default:
                    return "did something to you";
            }
        }

        /// <summary>
        /// Adds a reward ID to the list of created rewards
        /// </summary>
        /// <param name="rewardId">The ID of the created reward</param>
        public void TrackCreatedReward(string rewardId)
        {
            if (!createdRewardIds.Contains(rewardId))
            {
                createdRewardIds.Add(rewardId);
                Debug.Log($"Tracking reward ID: {rewardId} for cleanup");
            }
        }

        /// <summary>
        /// Deletes all channel points rewards created by the mod
        /// </summary>
        public void DeleteAllChannelPointsRewards()
        {
            Debug.Log("Starting cleanup of channel points rewards...");
            
            // Create a new list to avoid modification during enumeration
            foreach (string rewardId in new List<string>(createdRewardIds))
            {
                if (rewardsManager.DeleteReward(rewardId))
                {
                    createdRewardIds.Remove(rewardId);
                    pointsRewardsActions.Remove(rewardId);
                    Debug.Log($"Successfully deleted reward: {rewardId}");
                }
                else
                {
                    Debug.LogWarning($"Failed to delete reward: {rewardId}");
                }
            }
            
            // Clear any remaining entries
            createdRewardIds.Clear();
            pointsRewardsActions.Clear();
            
            Debug.Log("Finished cleanup of channel points rewards");
        }

        /// <summary>
        /// Closes the WebSocket connection and cleans up resources.
        /// Should be called when the mod is unloaded or when disconnecting from Twitch.
        /// </summary>
        public void Disconnect()
        {
            // Delete all rewards before disconnecting
            DeleteAllChannelPointsRewards();
            
            if (ws != null)
            {
                ws.Close();
                ws = null;
            }
        }
    }
}