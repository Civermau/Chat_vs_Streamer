using UnityEngine;
using System.Collections;

namespace Chat_vs_Streamer
{
    public class TextOnScreen : MonoBehaviour
    {
        private static TextOnScreen instance;
        
        // Text display properties
        private string currentText = "";
        private float displayDuration = 3.0f;
        private float fadeInTime = 0.5f;
        private float fadeOutTime = 0.5f;
        private float currentAlpha = 0f;
        private float displayTimer = 0f;
        private bool isDisplaying = false;
        private bool isFadingIn = false;
        private bool isFadingOut = false;
        
        // Style properties
        private GUIStyle textStyle;
        private Color textColor = Color.white;
        private int fontSize = 32;
        private FontStyle fontStyle = FontStyle.Bold;
        private Color outlineColor = Color.black;
        private float outlineWidth = 2f;
        
        // Queue of messages to display
        private Queue messageQueue = new Queue();
        
        private void Awake()
        {
            // Singleton pattern
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize text style
            textStyle = new GUIStyle();
            textStyle.fontSize = fontSize;
            textStyle.fontStyle = fontStyle;
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.normal.textColor = textColor;
        }
        
        private void OnGUI()
        {
            if (!isDisplaying && !isFadingIn && !isFadingOut)
                return;
                
            // Create a temporary color with current alpha
            Color tempColor = textColor;
            tempColor.a = currentAlpha;
            textStyle.normal.textColor = tempColor;
            
            // Calculate position for center of screen
            Vector2 textSize = textStyle.CalcSize(new GUIContent(currentText));
            Rect textRect = new Rect(
                (Screen.width - textSize.x) / 2,
                (Screen.height - textSize.y) / 2 - 50, // Slightly above center
                textSize.x,
                textSize.y
            );
            
            // Draw text outline
            DrawOutline(textRect, currentText, textStyle, outlineColor, outlineWidth);
            
            // Draw main text
            GUI.Label(textRect, currentText, textStyle);
        }
        
        private void DrawOutline(Rect rect, string text, GUIStyle style, Color outlineColor, float width)
        {
            // Save original color
            Color originalColor = style.normal.textColor;
            
            // Set outline color with current alpha
            Color tempOutlineColor = outlineColor;
            tempOutlineColor.a = originalColor.a;
            style.normal.textColor = tempOutlineColor;
            
            // Draw outline by drawing the text multiple times with small offsets
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue; // Skip center (will be drawn later)
                    
                    Rect offsetRect = new Rect(
                        rect.x + i * width,
                        rect.y + j * width,
                        rect.width,
                        rect.height
                    );
                    
                    GUI.Label(offsetRect, text, style);
                }
            }
            
            // Restore original color
            style.normal.textColor = originalColor;
        }
        
        private void Update()
        {
            if (isFadingIn)
            {
                // Fade in
                currentAlpha += Time.deltaTime / fadeInTime;
                if (currentAlpha >= 1f)
                {
                    currentAlpha = 1f;
                    isFadingIn = false;
                    isDisplaying = true;
                    displayTimer = displayDuration;
                }
            }
            else if (isDisplaying)
            {
                // Display for duration
                displayTimer -= Time.deltaTime;
                if (displayTimer <= 0f)
                {
                    isDisplaying = false;
                    isFadingOut = true;
                }
            }
            else if (isFadingOut)
            {
                // Fade out
                currentAlpha -= Time.deltaTime / fadeOutTime;
                if (currentAlpha <= 0f)
                {
                    currentAlpha = 0f;
                    isFadingOut = false;
                    
                    // Check if there are more messages in queue
                    if (messageQueue.Count > 0)
                    {
                        ShowNextMessage();
                    }
                }
            }
            else if (messageQueue.Count > 0 && !isDisplaying && !isFadingIn && !isFadingOut)
            {
                // If not displaying anything but have messages queued, show next
                ShowNextMessage();
            }
        }
        
        private void ShowNextMessage()
        {
            if (messageQueue.Count > 0)
            {
                currentText = (string)messageQueue.Dequeue();
                isFadingIn = true;
                currentAlpha = 0f;
            }
        }
        
        // Static methods to access from anywhere
        
        /// <summary>
        /// Display a message on screen for the specified duration
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="duration">Optional: How long to display (default: 3 seconds)</param>
        public static void ShowMessage(string message, float duration = 3.0f)
        {
            // Create the instance if it doesn't exist
            if (instance == null)
            {
                GameObject go = new GameObject("TextOnScreen");
                instance = go.AddComponent<TextOnScreen>();
            }
            
            // Update duration if specified
            if (duration != 3.0f)
            {
                instance.displayDuration = duration;
            }
            
            // Add message to queue
            instance.messageQueue.Enqueue(message);
            
            // If not currently showing anything, start showing this message
            if (!instance.isDisplaying && !instance.isFadingIn && !instance.isFadingOut)
            {
                instance.ShowNextMessage();
            }
        }
        
        /// <summary>
        /// Show a formatted message for user actions
        /// </summary>
        /// <param name="username">The name of the user</param>
        /// <param name="action">The action they performed</param>
        /// <param name="duration">Optional: How long to display (default: 3 seconds)</param>
        public static void ShowActionMessage(string username, string action, float duration = 3.0f)
        {
            ShowMessage($"{username} {action}!", duration);
        }
    }
}
