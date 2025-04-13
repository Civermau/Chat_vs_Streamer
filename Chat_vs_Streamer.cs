using RaftModLoader;
using UnityEngine;
using HMLLibrary;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System;
using Chat_vs_Streamer.Twitch;

namespace Chat_vs_Streamer
{
    public class Chat_vs_Streamer : Mod
    {
        private static TwitchWebSocketHandler twitchHandler;
        private static ChannelPointsRewardsManager channelPointsRewardsManager;
        private static List<string> rewardsCreated = new List<string>(); // This will store a list of all the rewards created by the mod so they can be deleted later
        private static ChannelPointsRewardsManagerWindow rewardsWindow;
        private static GameObject handlerObj;
        private static GameObject windowObj;
        private static GameObject textOnScreenObj;

        public void Start()
        {
            Debug.Log("Mod Chat_vs_Streamer has been loaded!");

            // Create a new GameObject to hold our components
            handlerObj = new GameObject("TwitchWebSocketHandler");
            windowObj = new GameObject("ChannelPointsRewardsManagerWindow");
            textOnScreenObj = new GameObject("TextOnScreen");

            // Add the components
            twitchHandler = handlerObj.AddComponent<TwitchWebSocketHandler>();
            rewardsWindow = windowObj.AddComponent<ChannelPointsRewardsManagerWindow>();
            textOnScreenObj.AddComponent<TextOnScreen>();

            // Don't destroy these objects when loading new scenes
            DontDestroyOnLoad(handlerObj);
            DontDestroyOnLoad(windowObj);
            DontDestroyOnLoad(textOnScreenObj);

            // Initialize Twitch handler with your credentials
            twitchHandler.Initialize(
                "731394949", // Let the user get their own channel ID
                "gy1ugssrqwqktg0pbf0kcogo90xqhw", // Let the user get their own OAuth token
                "llaipkuyl3rzkub31fy9wg6t6lynrg" // This is fixed, the app Id created on twitch's Developer Portal
            );

            // Connect to Twitch
            twitchHandler.Connect();

            // Initialize Channel Points Rewards Manager
            channelPointsRewardsManager = new ChannelPointsRewardsManager(
                "731394949", // Let the user get their own channel ID
                "gy1ugssrqwqktg0pbf0kcogo90xqhw", // Let the user get their own OAuth token
                "llaipkuyl3rzkub31fy9wg6t6lynrg" // This is fixed, the app Id created on twitch's Developer Portal
            );
        }

        public void OnModUnload()
        {
            if (twitchHandler != null)
            {
                twitchHandler.Disconnect();
                twitchHandler = null;
            }

            if (windowObj != null)
            {
                Destroy(windowObj);
                windowObj = null;
                rewardsWindow = null;
            }
            
            if (textOnScreenObj != null)
            {
                Destroy(textOnScreenObj);
                textOnScreenObj = null;
            }

            Debug.Log("Mod Chat_vs_Streamer has been unloaded!");
        }
    }
}