using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine.SceneManagement;

namespace Game
{
    public class InitAuth : MonoBehaviour
    {
        [SerializeField] private SceneReference _mainMenuScene;

        /// <summary>
        /// Try to sign in anonymously to the unity game services.
        /// Unity's game services recognices the player thanks to their pc parts. If parts have been changed then your profile might not match.
        /// </summary>
        private async void Start()
        {
            await UnityServices.InitializeAsync();

            if (UnityServices.State == ServicesInitializationState.Initialized)
            {
                AuthenticationService.Instance.SignedIn += SignedIn;
                AuthenticationService.Instance.SignInFailed += SignInFailed;
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            else
            {
                //Failed to init
                Debug.LogError("Initialization failed");
            }
        }

        /// <summary>
        /// Load into the main menu after signing in.
        /// </summary>
        private void SignedIn()
        {
            LoadScene.E_LoadScene.Invoke(_mainMenuScene);
            Debug.Log("Sign in completed");
            Debug.Log($"Player ID: {AuthenticationService.Instance.PlayerId}");
        }

        /// <summary>
        /// Sign in failed. Do nothing.
        /// </summary>
        /// <param name="exception"></param>
        private void SignInFailed(RequestFailedException exception)
        {
            Debug.LogError("Sign in failed");
            Debug.LogError(exception.Message);
        }
    }
}
