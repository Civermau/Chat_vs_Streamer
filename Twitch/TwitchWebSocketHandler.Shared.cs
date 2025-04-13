using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;
using System;

namespace Chat_vs_Streamer.Twitch
{
    public class ChannelPointsRewardsManagerWindow : MonoBehaviour
    {
        private bool showWindow = false;
        private Vector2 scrollPosition;
        private string[] rewardTypes = new string[] { "Channel Points Reward", "Bits Reward" };
        private int selectedRewardTypeIndex = 0;
        private string amountString = "100";
        private string rewardTitle = "";
        private string rewardPrompt = "";
        private string[] actionTypes;
        private int selectedActionTypeIndex = 0;
        private bool showActionDropdown = false;
        private List<JToken> rewards = new List<JToken>();
        private Rect windowRect = new Rect(20, 20, 600, 500);
        private bool showOnlyModRewards = true;

        // Edit mode variables
        private string editingRewardId = null;
        private string editTitle = "";
        private string editPrompt = "";
        private string editCost = "";
        private int editActionIndex = 0;

        // Bits edit mode variables
        private string editingBitsAmount = null;
        private string editBitsName = "";
        private int editBitsActionIndex = 0;

        // Custom dropdown implementation
        private bool showEditActionDropdown = false;
        private bool showEditBitsActionDropdown = false;

        private TwitchWebSocketHandler twitchHandler;

        private void Start()
        {
            // Get reference to TwitchWebSocketHandler
            twitchHandler = FindObjectOfType<TwitchWebSocketHandler>();
            if (twitchHandler == null)
            {
                Debug.LogError("TwitchWebSocketHandler reference is not set!");
            }
            
            // Initialize action types from enum
            actionTypes = GetActionTypesFromEnum();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                showWindow = !showWindow;
                if (showWindow)
                {
                    RefreshRewards();
                }
            }
        }

        private void OnGUI()
        {
            if (!showWindow) return;

            // Create a window with built-in dragging
            windowRect = GUI.Window(0, windowRect, DrawWindow, "Action Manager");
        }

        private void DrawWindow(int windowID)
        {
            GUILayout.BeginVertical();

            // Create new action section
            GUILayout.Label("Create New Action", GUI.skin.box);

            // Reward type dropdown (Channel Points or Bits)
            GUILayout.BeginHorizontal();
            GUILayout.Label("Type:", GUILayout.Width(50));
            selectedRewardTypeIndex = GUILayout.SelectionGrid(selectedRewardTypeIndex, rewardTypes, rewardTypes.Length);
            GUILayout.EndHorizontal();

            // Amount input (cost for points, amount for bits)
            GUILayout.BeginHorizontal();
            string amountLabel = selectedRewardTypeIndex == 0 ? "Cost:" : "Bits:";
            GUILayout.Label(amountLabel, GUILayout.Width(50));
            amountString = GUILayout.TextField(amountString);
            GUILayout.EndHorizontal();

            // For channel points, add title and prompt inputs
            if (selectedRewardTypeIndex == 0) // Channel Points
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Title:", GUILayout.Width(50));
                rewardTitle = GUILayout.TextField(rewardTitle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Prompt:", GUILayout.Width(50));
                rewardPrompt = GUILayout.TextField(rewardPrompt);
                GUILayout.EndHorizontal();
            }
            else // Bits
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Name:", GUILayout.Width(50));
                rewardTitle = GUILayout.TextField(rewardTitle);
                GUILayout.EndHorizontal();
            }

            // Action type dropdown
            GUILayout.BeginHorizontal();
            GUILayout.Label("Action:", GUILayout.Width(50));
            
            // Custom dropdown implementation
            if (GUILayout.Button($"Select Action ▼ ({actionTypes[selectedActionTypeIndex]})", GUILayout.MinWidth(200)))
            {
                showActionDropdown = !showActionDropdown;
                showEditActionDropdown = false;
                showEditBitsActionDropdown = false;
            }
            
            GUILayout.EndHorizontal();
            
            // Show dropdown content if active
            if (showActionDropdown)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                for (int i = 0; i < actionTypes.Length; i++)
                {
                    if (GUILayout.Button(actionTypes[i], GUILayout.MinWidth(150)))
                    {
                        selectedActionTypeIndex = i;
                        showActionDropdown = false;
                    }
                }
                GUILayout.EndVertical();
            }

            // Create button
            bool canCreate = int.TryParse(amountString, out int amount) && amount > 0;

            if (selectedRewardTypeIndex == 0) // Channel Points
            {
                canCreate = canCreate && !string.IsNullOrEmpty(rewardTitle) && !string.IsNullOrEmpty(rewardPrompt);
            }
            else // Bits
            {
                canCreate = canCreate && !string.IsNullOrEmpty(rewardTitle);
            }

            GUI.enabled = canCreate;
            if (GUILayout.Button("Create Action"))
            {
                if (selectedRewardTypeIndex == 0) // Channel Points
                {
                    // Create channel points reward and assign action
                    CreateChannelPointsAction(rewardTitle, amount, rewardPrompt);
                    RefreshRewards();
                }
                else // Bits
                {
                    // Create bits action
                    CreateBitsAction(amount, rewardTitle);
                }
            }
            GUI.enabled = true;

            if (!canCreate)
            {
                GUILayout.Label("Please fill all fields correctly", GUI.skin.box);
            }

            // List of existing rewards
            if (selectedRewardTypeIndex == 0) // Only show existing rewards for channel points
            {
                GUILayout.Label("Existing Channel Points Rewards", GUI.skin.box);

                // Add toggle for filtering rewards
                GUILayout.BeginHorizontal();
                GUILayout.Label("Show:", GUILayout.Width(50));
                showOnlyModRewards = GUILayout.Toggle(showOnlyModRewards, "Only Mod Rewards");
                GUILayout.EndHorizontal();

                scrollPosition = GUILayout.BeginScrollView(scrollPosition);

                // Sort rewards by cost
                var sortedRewards = rewards.OrderBy(r => int.Parse(r["cost"].ToString())).ToList();

                foreach (var reward in sortedRewards)
                {
                    // Skip rewards not created by the mod if showOnlyModRewards is true
                    if (showOnlyModRewards && !twitchHandler.pointsRewardsActions.ContainsKey(reward["id"].ToString()))
                    {
                        continue;
                    }

                    GUILayout.BeginVertical(GUI.skin.box);

                    // First row: Title and Cost
                    GUILayout.BeginHorizontal();

                    // Get the reward ID
                    string rewardId = reward["id"].ToString();

                    // Create a style for the title
                    var titleStyle = new GUIStyle(GUI.skin.label);
                    titleStyle.fontStyle = FontStyle.Bold;

                    // Display title
                    GUILayout.Label(reward["title"].ToString(), titleStyle, GUILayout.Width(250));

                    // Create a style for the cost
                    var costStyle = new GUIStyle(GUI.skin.label);
                    costStyle.normal.textColor = Color.yellow;

                    // Display cost
                    GUILayout.Label($"{reward["cost"]} points", costStyle, GUILayout.Width(100));

                    GUILayout.FlexibleSpace();

                    // Edit button with blue color
                    var editButtonStyle = new GUIStyle(GUI.skin.button);
                    editButtonStyle.normal.textColor = Color.cyan;

                    if (GUILayout.Button("Edit", editButtonStyle, GUILayout.Width(60)))
                    {
                        editingRewardId = rewardId;
                        editTitle = reward["title"].ToString();
                        editPrompt = reward["prompt"].ToString();
                        editCost = reward["cost"].ToString();
                        if (twitchHandler.pointsRewardsActions.ContainsKey(rewardId))
                        {
                            // Find the matching action type
                            var currentAction = twitchHandler.pointsRewardsActions[rewardId].others;
                            for (int i = 0; i < actionTypes.Length; i++)
                            {
                                if (actionTypes[i].Replace(" ", "") == currentAction.ToString())
                                {
                                    editActionIndex = i;
                                    break;
                                }
                            }
                        }
                    }

                    GUILayout.Space(10);

                    // Delete button with red color
                    var deleteButtonStyle = new GUIStyle(GUI.skin.button);
                    deleteButtonStyle.normal.textColor = Color.red;

                    if (GUILayout.Button("Delete", deleteButtonStyle, GUILayout.Width(60)))
                    {
                        twitchHandler.rewardsManager.DeleteReward(rewardId);
                        RefreshRewards();
                    }

                    GUILayout.EndHorizontal();

                    // Second row: Action (if exists)
                    // Check if this reward has an associated action
                    bool hasAction = twitchHandler.pointsRewardsActions.ContainsKey(rewardId);
                    if (hasAction)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);  // Indent the action

                        // Create a style for the action
                        var actionStyle = new GUIStyle(GUI.skin.label);
                        actionStyle.normal.textColor = Color.green;

                        // Display action with icon or symbol
                        GUILayout.Label($"⚡ Action: {twitchHandler.pointsRewardsActions[rewardId].others}", actionStyle);

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.EndVertical();

                    // Add some spacing between rewards
                    GUILayout.Space(5);
                }

                // Edit interface
                if (editingRewardId != null)
                {
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label("Edit Reward", GUI.skin.box);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Title:", GUILayout.Width(50));
                    editTitle = GUILayout.TextField(editTitle);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Prompt:", GUILayout.Width(50));
                    editPrompt = GUILayout.TextField(editPrompt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Cost:", GUILayout.Width(50));
                    editCost = GUILayout.TextField(editCost);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Action:", GUILayout.Width(50));
                    
                    // Custom dropdown for edit mode
                    if (GUILayout.Button($"Select Action ▼ ({actionTypes[editActionIndex]})", GUILayout.MinWidth(200)))
                    {
                        showEditActionDropdown = !showEditActionDropdown;
                        showActionDropdown = false;
                        showEditBitsActionDropdown = false;
                    }
                    
                    GUILayout.EndHorizontal();
                    
                    // Show dropdown content if active
                    if (showEditActionDropdown)
                    {
                        GUILayout.BeginVertical(GUI.skin.box);
                        for (int i = 0; i < actionTypes.Length; i++)
                        {
                            if (GUILayout.Button(actionTypes[i], GUILayout.MinWidth(150)))
                            {
                                editActionIndex = i;
                                showEditActionDropdown = false;
                            }
                        }
                        GUILayout.EndVertical();
                    }

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Save"))
                    {
                        if (int.TryParse(editCost, out int newCost) && newCost > 0)
                        {
                            // Update the reward on Twitch
                            var response = twitchHandler.rewardsManager.UpdateReward(
                                editingRewardId,
                                editTitle,
                                newCost,
                                editPrompt
                            );

                            if (response != null)
                            {
                                // Update the action
                                string actionName = actionTypes[editActionIndex].Replace(" ", "");
                                Action newAction = new Action(Action.ActionType.Others,
                                    (Action.Others)Enum.Parse(typeof(Action.Others), actionName));
                                twitchHandler.pointsRewardsActions[editingRewardId] = newAction;

                                // Reset edit mode
                                editingRewardId = null;
                                RefreshRewards();
                            }
                        }
                    }

                    if (GUILayout.Button("Cancel"))
                    {
                        editingRewardId = null;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();
                }

                GUILayout.EndScrollView();
            }
            else // Bits rewards list
            {
                GUILayout.Label("Existing Bits Rewards", GUI.skin.box);
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);

                // Convert dictionary to sorted list by bits amount
                var sortedBitsRewards = twitchHandler.bitsRewardsActions
                    .OrderBy(pair => int.Parse(pair.Key))
                    .ToList();

                foreach (var bitsReward in sortedBitsRewards)
                {
                    GUILayout.BeginVertical(GUI.skin.box);

                    // First row: Name, Bits amount, and buttons
                    GUILayout.BeginHorizontal();

                    // Create styles
                    var nameStyle = new GUIStyle(GUI.skin.label);
                    nameStyle.fontStyle = FontStyle.Bold;

                    var bitsStyle = new GUIStyle(GUI.skin.label);
                    bitsStyle.normal.textColor = Color.magenta;

                    // Display name (or bits amount if no name)
                    string rewardName = bitsReward.Value.name ?? $"Bits Reward {bitsReward.Key}";
                    GUILayout.Label(rewardName, nameStyle, GUILayout.Width(250));

                    // Display bits amount
                    GUILayout.Label($"{bitsReward.Key} bits", bitsStyle, GUILayout.Width(100));

                    GUILayout.FlexibleSpace();

                    // Edit button
                    var editButtonStyle = new GUIStyle(GUI.skin.button);
                    editButtonStyle.normal.textColor = Color.cyan;

                    if (GUILayout.Button("Edit", editButtonStyle, GUILayout.Width(60)))
                    {
                        editingBitsAmount = bitsReward.Key;
                        editBitsName = bitsReward.Value.name ?? "";

                        // Find matching action type
                        var currentAction = bitsReward.Value.others;
                        for (int i = 0; i < actionTypes.Length; i++)
                        {
                            if (actionTypes[i].Replace(" ", "") == currentAction.ToString())
                            {
                                editBitsActionIndex = i;
                                break;
                            }
                        }
                    }

                    GUILayout.Space(10);

                    // Delete button
                    var deleteButtonStyle = new GUIStyle(GUI.skin.button);
                    deleteButtonStyle.normal.textColor = Color.red;

                    if (GUILayout.Button("Delete", deleteButtonStyle, GUILayout.Width(60)))
                    {
                        twitchHandler.bitsRewardsActions.Remove(bitsReward.Key);
                    }

                    GUILayout.EndHorizontal();

                    // Second row: Action
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);

                    var actionStyle = new GUIStyle(GUI.skin.label);
                    actionStyle.normal.textColor = Color.green;
                    GUILayout.Label($"⚡ Action: {bitsReward.Value.others}", actionStyle);

                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();

                    GUILayout.Space(5);
                }

                // Bits reward edit interface
                if (editingBitsAmount != null)
                {
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label("Edit Bits Reward", GUI.skin.box);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Name:", GUILayout.Width(50));
                    editBitsName = GUILayout.TextField(editBitsName);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Action:", GUILayout.Width(50));
                    
                    // Custom dropdown for bits edit mode
                    if (GUILayout.Button($"Select Action ▼ ({actionTypes[editBitsActionIndex]})", GUILayout.MinWidth(200)))
                    {
                        showEditBitsActionDropdown = !showEditBitsActionDropdown;
                        showActionDropdown = false;
                        showEditActionDropdown = false;
                    }
                    
                    GUILayout.EndHorizontal();
                    
                    // Show dropdown content if active
                    if (showEditBitsActionDropdown)
                    {
                        GUILayout.BeginVertical(GUI.skin.box);
                        for (int i = 0; i < actionTypes.Length; i++)
                        {
                            if (GUILayout.Button(actionTypes[i], GUILayout.MinWidth(150)))
                            {
                                editBitsActionIndex = i;
                                showEditBitsActionDropdown = false;
                            }
                        }
                        GUILayout.EndVertical();
                    }

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Save"))
                    {
                        string actionName = actionTypes[editBitsActionIndex].Replace(" ", "");
                        Action newAction = new Action(Action.ActionType.Others,
                            (Action.Others)Enum.Parse(typeof(Action.Others), actionName))
                        {
                            name = editBitsName
                        };

                        twitchHandler.bitsRewardsActions[editingBitsAmount] = newAction;
                        editingBitsAmount = null;
                    }

                    if (GUILayout.Button("Cancel"))
                    {
                        editingBitsAmount = null;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();

            // Allow the window to be dragged by its title bar
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void CreateChannelPointsAction(string title, int cost, string prompt)
        {
            Debug.Log($"Creating channel points action: Title={title}, Cost={cost}, Prompt={prompt}");

            // First create the channel point reward
            var response = twitchHandler.rewardsManager.CreateReward(title, cost, prompt);
            if (response == null)
            {
                Debug.LogError("Failed to create reward");
                return;
            }

            // Get the ID of the newly created reward
            var rewardId = JObject.Parse(response)["data"][0]["id"].ToString();
            Debug.Log($"Created reward with ID: {rewardId}");

            if (rewardId != null && twitchHandler != null)
            {
                // Create the action based on selection
                Action action = CreateActionFromSelection();
                Debug.Log($"Created action of type: {action.others}");

                // Add to the dictionary directly
                if (!twitchHandler.pointsRewardsActions.ContainsKey(rewardId))
                {
                    twitchHandler.pointsRewardsActions.Add(rewardId, action);
                    // Track the created reward for cleanup
                    twitchHandler.TrackCreatedReward(rewardId);
                    Debug.Log($"Added channel points action to dictionary. Current dictionary size: {twitchHandler.pointsRewardsActions.Count}");
                }
                else
                {
                    twitchHandler.pointsRewardsActions[rewardId] = action;
                    Debug.Log($"Updated channel points action in dictionary. Current dictionary size: {twitchHandler.pointsRewardsActions.Count}");
                }
            }
            else
            {
                Debug.LogError($"Failed to create channel points action. RewardId: {rewardId}, TwitchHandler: {twitchHandler != null}");
            }
        }

        private void CreateBitsAction(int amount, string name)
        {
            Debug.Log($"Creating bits action for amount: {amount}");

            if (twitchHandler != null)
            {
                // Create the action based on selection
                Action action = CreateActionFromSelection();
                action.name = name;  // Set the name
                Debug.Log($"Created action of type: {action.others}");

                // Add to the dictionary directly
                string amountKey = amount.ToString();
                if (!twitchHandler.bitsRewardsActions.ContainsKey(amountKey))
                {
                    twitchHandler.bitsRewardsActions.Add(amountKey, action);
                    Debug.Log($"Added bits action to dictionary. Current dictionary size: {twitchHandler.bitsRewardsActions.Count}");
                }
                else
                {
                    twitchHandler.bitsRewardsActions[amountKey] = action;
                    Debug.Log($"Updated bits action in dictionary. Current dictionary size: {twitchHandler.bitsRewardsActions.Count}");
                }
            }
            else
            {
                Debug.LogError("Failed to create bits action. TwitchHandler is null");
            }
        }

        private Action CreateActionFromSelection()
        {
            // Get the enum value directly using the name without spaces
            string actionName = actionTypes[selectedActionTypeIndex].Replace(" ", "");
            Action.Others selectedOther = (Action.Others)Enum.Parse(typeof(Action.Others), actionName);
            
            return new Action(Action.ActionType.Others, selectedOther);
        }

        private void RefreshRewards()
        {
            var response = twitchHandler.rewardsManager.GetRewards();
            if (response != null)
            {
                var gottenRewards = JObject.Parse(response);
                rewards = gottenRewards["data"].ToObject<List<JToken>>();
            }
        }

        private string[] GetActionTypesFromEnum()
        {
            // Get all values from the Action.Others enum
            Array enumValues = Enum.GetValues(typeof(Action.Others));
            string[] result = new string[enumValues.Length];
            
            // Convert enum values to display strings (adding spaces for readability)
            for (int i = 0; i < enumValues.Length; i++)
            {
                string enumValueString = enumValues.GetValue(i).ToString();
                // Add spaces before capital letters to make it more readable
                result[i] = System.Text.RegularExpressions.Regex.Replace(
                    enumValueString, 
                    "([a-z])([A-Z])", 
                    "$1 $2"
                );
            }
            
            return result;
        }
    }
}