using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TowerClimb.Core;

namespace TowerClimb.API
{
    /// <summary>
    /// Supabase API client for Unity
    /// Handles auth, run submission, and leaderboard fetching
    /// </summary>
    public class SupabaseClient : MonoBehaviour
    {
        public static SupabaseClient Instance { get; private set; }

        [Header("Configuration")]
        public string supabaseUrl = "https://your-project.supabase.co";
        public string supabaseAnonKey = "your-anon-key";

        [Header("State")]
        public string userId;
        public string accessToken;
        public bool isAuthenticated;

        private const string PREFS_USER_ID = "TowerClimb_UserId";
        private const string PREFS_ACCESS_TOKEN = "TowerClimb_AccessToken";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            LoadSession();
        }

        /// <summary>
        /// Load saved session from PlayerPrefs
        /// </summary>
        private void LoadSession()
        {
            userId = PlayerPrefs.GetString(PREFS_USER_ID, "");
            accessToken = PlayerPrefs.GetString(PREFS_ACCESS_TOKEN, "");
            isAuthenticated = !string.IsNullOrEmpty(accessToken);

            if (isAuthenticated)
            {
                Debug.Log($"[SupabaseClient] Loaded session for user: {userId}");
            }
        }

        /// <summary>
        /// Save session to PlayerPrefs
        /// </summary>
        private void SaveSession()
        {
            PlayerPrefs.SetString(PREFS_USER_ID, userId);
            PlayerPrefs.SetString(PREFS_ACCESS_TOKEN, accessToken);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Sign in anonymously (for guest play)
        /// </summary>
        public IEnumerator SignInAnonymously(Action<bool, string> callback)
        {
            string url = $"{supabaseUrl}/auth/v1/signup";

            // Generate a random anonymous email
            string anonymousId = System.Guid.NewGuid().ToString();
            string jsonData = $"{{\"email\":\"{anonymousId}@anon.tower-climb.com\",\"password\":\"{anonymousId}\"}}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", supabaseAnonKey);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    var response = JsonUtility.FromJson<AuthResponse>(responseText);

                    userId = response.user.id;
                    accessToken = response.access_token;
                    isAuthenticated = true;

                    SaveSession();

                    Debug.Log($"[SupabaseClient] Anonymous sign in successful: {userId}");
                    callback?.Invoke(true, userId);
                }
                else
                {
                    Debug.LogError($"[SupabaseClient] Sign in failed: {request.error}");
                    callback?.Invoke(false, request.error);
                }
            }
        }

        /// <summary>
        /// Start a new run - fetch current season and seed
        /// </summary>
        public IEnumerator StartRun(Action<SessionData, string> callback)
        {
            if (!isAuthenticated)
            {
                callback?.Invoke(null, "Not authenticated");
                yield break;
            }

            string url = $"{supabaseUrl}/functions/v1/start-run";

            using (UnityWebRequest request = UnityWebRequest.Post(url, ""))
            {
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                request.SetRequestHeader("apikey", supabaseAnonKey);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    var sessionData = JsonUtility.FromJson<SessionData>(responseText);

                    Debug.Log($"[SupabaseClient] Run started: Week {sessionData.weekId}, Seed {sessionData.seed}");
                    callback?.Invoke(sessionData, null);
                }
                else
                {
                    Debug.LogError($"[SupabaseClient] Start run failed: {request.error}");
                    callback?.Invoke(null, request.error);
                }
            }
        }

        /// <summary>
        /// Submit completed run to server for validation and leaderboard update
        /// </summary>
        public IEnumerator SubmitRun(RunSubmission submission, Action<SubmitRunResponse, string> callback)
        {
            if (!isAuthenticated)
            {
                callback?.Invoke(null, "Not authenticated");
                yield break;
            }

            string url = $"{supabaseUrl}/functions/v1/submit-run";

            // Serialize submission to JSON
            // NOTE: Unity's JsonUtility doesn't handle complex objects well, so we build JSON manually
            string jsonData = BuildSubmissionJson(submission);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                request.SetRequestHeader("apikey", supabaseAnonKey);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    var response = JsonUtility.FromJson<SubmitRunResponse>(responseText);

                    Debug.Log($"[SupabaseClient] Run submitted: NewBest={response.newBest}, CheatFlags={response.cheatFlags}");
                    callback?.Invoke(response, null);
                }
                else
                {
                    Debug.LogError($"[SupabaseClient] Submit run failed: {request.error}\n{request.downloadHandler.text}");
                    callback?.Invoke(null, request.error);
                }
            }
        }

        /// <summary>
        /// Fetch leaderboard
        /// </summary>
        public IEnumerator GetLeaderboard(int? weekId, string scope, int limit, Action<LeaderboardResponse, string> callback)
        {
            string url = $"{supabaseUrl}/functions/v1/get-leaderboard?scope={scope}&limit={limit}";
            if (weekId.HasValue)
            {
                url += $"&weekId={weekId.Value}";
            }

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                if (isAuthenticated)
                {
                    request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                }
                request.SetRequestHeader("apikey", supabaseAnonKey);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    var response = JsonUtility.FromJson<LeaderboardResponse>(responseText);

                    Debug.Log($"[SupabaseClient] Leaderboard fetched: {response.entries.Length} entries");
                    callback?.Invoke(response, null);
                }
                else
                {
                    Debug.LogError($"[SupabaseClient] Get leaderboard failed: {request.error}");
                    callback?.Invoke(null, request.error);
                }
            }
        }

        /// <summary>
        /// Build JSON for run submission (manual serialization for complex types)
        /// </summary>
        private string BuildSubmissionJson(RunSubmission submission)
        {
            StringBuilder json = new StringBuilder();
            json.Append("{");
            json.Append($"\"weekId\":{submission.weekId},");
            json.Append($"\"floors\":{submission.floors},");
            json.Append($"\"runtimeSeconds\":{submission.runtimeSeconds},");
            json.Append($"\"avgReactionMs\":{submission.avgReactionMs},");
            json.Append($"\"clientVersion\":\"{submission.clientVersion}\",");

            // Breakdown (simplified)
            json.Append("\"breakdown\":{},");

            // Timings array
            json.Append("\"timings\":[");
            for (int i = 0; i < submission.timings.Count; i++)
            {
                var timing = submission.timings[i];
                json.Append("{");
                json.Append($"\"floor\":{timing.floor},");
                json.Append($"\"patternType\":\"{timing.patternType.ToString().ToLower()}\",");
                json.Append($"\"reactionMs\":{timing.reactionMs},");
                json.Append($"\"success\":{timing.success.ToString().ToLower()},");
                json.Append($"\"accuracy\":{timing.accuracy},");
                json.Append($"\"timestamp\":{timing.timestamp}");
                json.Append("}");
                if (i < submission.timings.Count - 1) json.Append(",");
            }
            json.Append("],");

            // Player model
            json.Append("\"playerModel\":{\"weaknesses\":{},\"last5\":[]}");

            json.Append("}");

            return json.ToString();
        }
    }

    // Response data structures
    [Serializable]
    public class AuthResponse
    {
        public string access_token;
        public AuthUser user;
    }

    [Serializable]
    public class AuthUser
    {
        public string id;
    }

    [Serializable]
    public class SubmitRunResponse
    {
        public bool success;
        public int cheatFlags;
        public bool newBest;
        public string[] unlocks;
    }

    [Serializable]
    public class LeaderboardResponse
    {
        public int weekId;
        public string scope;
        public LeaderboardEntry[] entries;
        public LeaderboardEntry userEntry;
        public bool hasMore;
    }
}
