using System.Collections;
using UnityEngine;
using TowerClimb.Core;
using TowerClimb.Gameplay;

namespace TowerClimb.API
{
    /// <summary>
    /// Manages game session flow: auth → start run → play → submit → leaderboard
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        [Header("References")]
        private SupabaseClient supabaseClient;
        private GameStateMachine gameStateMachine;

        [Header("Current Session")]
        public SessionData currentSession;
        public bool isSessionActive;

        // Events
        public delegate void SessionEventHandler(bool success, string error);
        public event SessionEventHandler OnAuthComplete;
        public event SessionEventHandler OnRunStarted;
        public event SessionEventHandler OnRunSubmitted;

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
                return;
            }
        }

        private void Start()
        {
            // Get singleton instances with null checks
            supabaseClient = SupabaseClient.Instance;
            if (supabaseClient == null)
            {
                Debug.LogError("[SessionManager] SupabaseClient not found in scene! Add SupabaseClient component.");
                return;
            }

            gameStateMachine = GameStateMachine.Instance;
            if (gameStateMachine != null)
            {
                gameStateMachine.OnRunEnded += HandleRunEnded;
            }
            else
            {
                Debug.LogWarning("[SessionManager] GameStateMachine not found - it will be created when needed");
            }

            // Auto-authenticate if not already
            if (!supabaseClient.isAuthenticated)
            {
                StartCoroutine(Authenticate());
            }
        }

        /// <summary>
        /// Authenticate user (anonymous or saved session)
        /// </summary>
        public IEnumerator Authenticate()
        {
            Debug.Log("[SessionManager] Authenticating...");

            if (supabaseClient.isAuthenticated)
            {
                Debug.Log("[SessionManager] Already authenticated");
                OnAuthComplete?.Invoke(true, null);
                yield break;
            }

            yield return StartCoroutine(supabaseClient.SignInAnonymously((success, error) =>
            {
                if (success)
                {
                    Debug.Log($"[SessionManager] Authentication successful: {supabaseClient.userId}");
                    OnAuthComplete?.Invoke(true, null);
                }
                else
                {
                    Debug.LogError($"[SessionManager] Authentication failed: {error}");
                    OnAuthComplete?.Invoke(false, error);
                }
            }));
        }

        /// <summary>
        /// Start a new run - fetch seed from server and initialize game
        /// </summary>
        public void StartNewRun()
        {
            if (!supabaseClient.isAuthenticated)
            {
                Debug.LogWarning("[SessionManager] Cannot start run - not authenticated");
                StartCoroutine(Authenticate());
                return;
            }

            StartCoroutine(StartRunCoroutine());
        }

        private IEnumerator StartRunCoroutine()
        {
            Debug.Log("[SessionManager] Starting new run...");

            yield return StartCoroutine(supabaseClient.StartRun((sessionData, error) =>
            {
                if (sessionData != null)
                {
                    currentSession = sessionData;
                    isSessionActive = true;

                    Debug.Log($"[SessionManager] Run started: Week {sessionData.weekId}, Seed {sessionData.seed}");

                    // Initialize game with server seed
                    if (gameStateMachine != null)
                    {
                        gameStateMachine.InitializeRun(sessionData.seed, sessionData.weekId);
                    }

                    OnRunStarted?.Invoke(true, null);
                }
                else
                {
                    Debug.LogError($"[SessionManager] Failed to start run: {error}");
                    OnRunStarted?.Invoke(false, error);
                }
            }));
        }

        /// <summary>
        /// Called when run ends - submit results to server
        /// </summary>
        private void HandleRunEnded()
        {
            if (!isSessionActive)
            {
                Debug.LogWarning("[SessionManager] Run ended but no active session");
                return;
            }

            StartCoroutine(SubmitRunCoroutine());
        }

        private IEnumerator SubmitRunCoroutine()
        {
            Debug.Log("[SessionManager] Submitting run...");

            // Build submission from game state
            var stats = gameStateMachine.GetRunStats();

            var submission = new RunSubmission
            {
                weekId = currentSession.weekId,
                floors = stats.floors,
                runtimeSeconds = stats.runtimeSeconds,
                avgReactionMs = stats.avgReactionMs,
                breakdown = new System.Collections.Generic.Dictionary<PatternType, PatternBreakdown>(),
                timings = gameStateMachine.runTimings,
                playerModel = gameStateMachine.playerModel,
                clientVersion = Application.version
            };

            yield return StartCoroutine(supabaseClient.SubmitRun(submission, (response, error) =>
            {
                if (response != null && response.success)
                {
                    Debug.Log($"[SessionManager] Run submitted successfully!");
                    Debug.Log($"  - New Best: {response.newBest}");
                    Debug.Log($"  - Cheat Flags: {response.cheatFlags}");
                    Debug.Log($"  - Unlocks: {(response.unlocks != null ? response.unlocks.Length : 0)}");

                    OnRunSubmitted?.Invoke(true, null);

                    // Show unlocks if any
                    if (response.unlocks != null && response.unlocks.Length > 0)
                    {
                        ShowUnlocks(response.unlocks);
                    }
                }
                else
                {
                    Debug.LogError($"[SessionManager] Failed to submit run: {error}");
                    OnRunSubmitted?.Invoke(false, error);
                }
            }));

            isSessionActive = false;
        }

        /// <summary>
        /// Fetch leaderboard for current or specific week
        /// </summary>
        public void FetchLeaderboard(int? weekId = null, string scope = "global", System.Action<LeaderboardResponse> callback = null)
        {
            StartCoroutine(FetchLeaderboardCoroutine(weekId, scope, callback));
        }

        private IEnumerator FetchLeaderboardCoroutine(int? weekId, string scope, System.Action<LeaderboardResponse> callback)
        {
            Debug.Log($"[SessionManager] Fetching leaderboard: week={weekId}, scope={scope}");

            yield return StartCoroutine(supabaseClient.GetLeaderboard(weekId, scope, 100, (response, error) =>
            {
                if (response != null)
                {
                    Debug.Log($"[SessionManager] Leaderboard loaded: {response.entries.Length} entries");
                    callback?.Invoke(response);
                }
                else
                {
                    Debug.LogError($"[SessionManager] Failed to fetch leaderboard: {error}");
                    callback?.Invoke(null);
                }
            }));
        }

        /// <summary>
        /// Display unlock notifications
        /// </summary>
        private void ShowUnlocks(string[] unlocks)
        {
            foreach (string unlock in unlocks)
            {
                Debug.Log($"[SessionManager] 🎉 Unlocked: {unlock}");
                // TODO: Show UI notification
            }
        }

        private void OnDestroy()
        {
            if (gameStateMachine != null)
            {
                gameStateMachine.OnRunEnded -= HandleRunEnded;
            }
        }
    }
}
