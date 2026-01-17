using UnityEngine;
using UnityEngine.UI;
using TMPro; // Make sure to include TextMeshPro namespace

namespace LoginSystem // Use your existing namespace, or create a new one if preferred
{
    public class PasswordVisibilityToggle : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The TMP_InputField whose content type will be toggled.")]
        public TMP_InputField targetInputField;

        [Tooltip("The Image component of the eye icon (e.g., on the Button itself).")]
        public Image eyeIconImage;

        [Tooltip("Sprite for when the password is visible (open eye).")]
        public Sprite eyeOpenSprite;

        [Tooltip("Sprite for when the password is hidden (closed eye).")]
        public Sprite eyeClosedSprite;

        private bool isPasswordVisible = false; // Initial state: password hidden

        /// <summary>
        /// Toggles the visibility of the password in the targetInputField.
        /// This method should be called by the OnClick() event of the eye icon button.
        /// </summary>
        public void ToggleVisibility()
        {
            if (targetInputField == null || eyeIconImage == null || eyeOpenSprite == null || eyeClosedSprite == null)
            {
                Debug.LogError("PasswordVisibilityToggle: Missing required references. Please assign Target Input Field, Eye Icon Image, and both Sprites in the Inspector.");
                return;
            }

            isPasswordVisible = !isPasswordVisible; // Toggle the state

            if (isPasswordVisible)
            {
                // Show password
                targetInputField.contentType = TMP_InputField.ContentType.Standard;
                eyeIconImage.sprite = eyeOpenSprite;
            }
            else
            {
                // Hide password
                targetInputField.contentType = TMP_InputField.ContentType.Password;
                eyeIconImage.sprite = eyeClosedSprite;
            }

            // IMPORTANT: Re-assign text to force the input field to redraw
            // and apply the new content type rules correctly.
            // This is crucial for TMP_InputField to update its display properly.
            targetInputField.text = targetInputField.text;

            // Optionally, re-focus the input field after toggling for better UX
            targetInputField.Select();
            targetInputField.ActivateInputField();
        }
    }
}