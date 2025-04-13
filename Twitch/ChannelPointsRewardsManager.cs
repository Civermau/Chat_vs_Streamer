using System;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Chat_vs_Streamer.Twitch
{
    /// <summary>
    /// Manages CRUD operations for Twitch channel points rewards.
    /// Handles creating, reading, updating, and deleting custom rewards.
    /// </summary>
    /// <remarks>
    /// See Api-Usage.md for more info and examples.
    /// </remarks>
    public class ChannelPointsRewardsManager
    {
        private string channelId;
        private string oauthToken;
        private string clientId;

        /// <summary>
        /// Class constructor, sets the credentials for the Twitch API
        /// </summary>
        /// <param name="channelId">Twitch channel ID</param>
        /// <param name="oauthToken">Twitch OAuth token</param>
        /// <param name="clientId">Twitch app client ID</param>
        public ChannelPointsRewardsManager(string channelId, string oauthToken, string clientId)
        {
            this.channelId = channelId;
            this.oauthToken = oauthToken;
            this.clientId = clientId;
        }

        /// <summary>
        /// Creates a new custom reward
        /// </summary>
        /// <param name="title">Title of the reward</param>
        /// <param name="cost">Cost in channel points</param>
        /// <param name="prompt">Optional prompt shown to users</param>
        /// <param name="isEnabled">Whether the reward is enabled</param>
        /// <returns>Response from Twitch API</returns>
        public string CreateReward(string title, int cost, string prompt = "")
        {
            try
            {
                var reward = new
                {
                    title = title,
                    cost = cost,
                    prompt = prompt,
                    is_enabled = true
                };

                var json = JsonConvert.SerializeObject(reward);

                using (var client = new WebClient())
                {
                    client.Headers.Add("Client-Id", clientId);
                    client.Headers.Add("Authorization", $"Bearer {oauthToken}");
                    client.Headers.Add("Content-Type", "application/json");

                    var response = client.UploadString(
                        $"https://api.twitch.tv/helix/channel_points/custom_rewards?broadcaster_id={channelId}",
                        "POST",
                        json
                    );
                    return response;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating reward: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets all custom rewards for the channel
        /// </summary>
        /// <returns>Response from Twitch API containing all rewards</returns>
        public string GetRewards()
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Headers.Add("Client-Id", clientId);
                    client.Headers.Add("Authorization", $"Bearer {oauthToken}");

                    var response = client.DownloadString(
                        $"https://api.twitch.tv/helix/channel_points/custom_rewards?broadcaster_id={channelId}"
                    );
                    return response;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error getting rewards: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Updates an existing custom reward
        /// </summary>
        /// <param name="rewardId">ID of the reward to update</param>
        /// <param name="title">New title (optional)</param>
        /// <param name="cost">New cost (optional)</param>
        /// <param name="prompt">New prompt (optional)</param>
        /// <param name="isEnabled">New enabled state (optional)</param>
        /// <returns>Response from Twitch API</returns>
        public string UpdateReward(string rewardId, string title = null, int? cost = null, string prompt = null, bool? isEnabled = null)
        {
            try
            {
                var updateData = new JObject();
                if (title != null) updateData["title"] = title;
                if (cost.HasValue) updateData["cost"] = cost.Value;
                if (prompt != null) updateData["prompt"] = prompt;
                if (isEnabled.HasValue) updateData["is_enabled"] = isEnabled.Value;

                using (var client = new WebClient())
                {
                    client.Headers.Add("Client-Id", clientId);
                    client.Headers.Add("Authorization", $"Bearer {oauthToken}");
                    client.Headers.Add("Content-Type", "application/json");

                    var response = client.UploadString(
                        $"https://api.twitch.tv/helix/channel_points/custom_rewards?broadcaster_id={channelId}&id={rewardId}",
                        "PATCH",
                        updateData.ToString()
                    );
                    return response;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating reward: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deletes a custom reward
        /// </summary>
        /// <param name="rewardId">ID of the reward to delete</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool DeleteReward(string rewardId)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Headers.Add("Client-Id", clientId);
                    client.Headers.Add("Authorization", $"Bearer {oauthToken}");

                    client.UploadString(
                        $"https://api.twitch.tv/helix/channel_points/custom_rewards?broadcaster_id={channelId}&id={rewardId}",
                        "DELETE",
                        ""
                    );
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error deleting reward: {ex.Message}");
                return false;
            }
        }
    }
} 