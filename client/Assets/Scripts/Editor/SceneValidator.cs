#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using TowerClimb.Core;
using TowerClimb.Gameplay;
using TowerClimb.UI;
using TowerClimb.Analytics;
using System.Collections.Generic;

namespace TowerClimb.Editor
{
    /// <summary>
    /// Unity Editor tool to validate scene setup
    /// Menu: Tools > Tower Climb > Validate Scene Setup
    /// </summary>
    public class SceneValidator : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<ValidationResult> results = new List<ValidationResult>();

        [MenuItem("Tools/Tower Climb/Validate Scene Setup")]
        public static void ShowWindow()
        {
            GetWindow<SceneValidator>("Scene Validator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Tower Climb Scene Validator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Run Validation", GUILayout.Height(40)))
            {
                RunValidation();
            }

            GUILayout.Space(20);

            // Display results
            if (results.Count > 0)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                int passCount = 0;
                int failCount = 0;
                int warnCount = 0;

                foreach (var result in results)
                {
                    DrawResult(result);

                    if (result.status == ValidationStatus.Pass) passCount++;
                    else if (result.status == ValidationStatus.Fail) failCount++;
                    else if (result.status == ValidationStatus.Warning) warnCount++;
                }

                EditorGUILayout.EndScrollView();

                GUILayout.Space(10);
                GUILayout.Label($"Results: {passCount} passed, {failCount} failed, {warnCount} warnings", EditorStyles.boldLabel);

                if (failCount == 0 && warnCount == 0)
                {
                    EditorGUILayout.HelpBox("Scene is fully configured! You're ready to build and test.", MessageType.Info);
                }
            }
        }

        private void DrawResult(ValidationResult result)
        {
            GUIStyle style = new GUIStyle(EditorStyles.helpBox);
            Color bgColor = result.status switch
            {
                ValidationStatus.Pass => new Color(0.2f, 0.8f, 0.2f, 0.2f),
                ValidationStatus.Fail => new Color(0.8f, 0.2f, 0.2f, 0.2f),
                ValidationStatus.Warning => new Color(0.8f, 0.8f, 0.2f, 0.2f),
                _ => Color.white
            };

            GUI.backgroundColor = bgColor;

            GUILayout.BeginVertical(style);

            string icon = result.status switch
            {
                ValidationStatus.Pass => "✓",
                ValidationStatus.Fail => "✗",
                ValidationStatus.Warning => "⚠",
                _ => "?"
            };

            GUILayout.Label($"{icon} {result.category}: {result.message}");

            if (!string.IsNullOrEmpty(result.details))
            {
                GUIStyle detailStyle = new GUIStyle(EditorStyles.label);
                detailStyle.fontSize = 10;
                detailStyle.normal.textColor = Color.gray;
                GUILayout.Label(result.details, detailStyle);
            }

            GUILayout.EndVertical();
            GUI.backgroundColor = Color.white;

            GUILayout.Space(5);
        }

        private void RunValidation()
        {
            results.Clear();

            ValidateManagers();
            ValidateUIScreens();
            ValidateCanvas();
            ValidatePrefabs();
            ValidateAssets();

            Repaint();
        }

        #region Validation Methods

        private void ValidateManagers()
        {
            // GameStateMachine
            var gameStateMachine = FindObjectOfType<GameStateMachine>();
            if (gameStateMachine != null)
            {
                results.Add(new ValidationResult
                {
                    category = "Managers",
                    status = ValidationStatus.Pass,
                    message = "GameStateMachine found"
                });

                // Check difficulty config
                if (gameStateMachine.difficultyConfig != null)
                {
                    results.Add(new ValidationResult
                    {
                        category = "Managers",
                        status = ValidationStatus.Pass,
                        message = "Difficulty config is set"
                    });
                }
                else
                {
                    results.Add(new ValidationResult
                    {
                        category = "Managers",
                        status = ValidationStatus.Warning,
                        message = "Difficulty config is null (will use defaults)"
                    });
                }
            }
            else
            {
                results.Add(new ValidationResult
                {
                    category = "Managers",
                    status = ValidationStatus.Fail,
                    message = "GameStateMachine not found in scene",
                    details = "Create an empty GameObject and add GameStateMachine.cs"
                });
            }

            // SessionManager
            var sessionManager = FindObjectOfType<SessionManager>();
            if (sessionManager != null)
            {
                results.Add(new ValidationResult
                {
                    category = "Managers",
                    status = ValidationStatus.Pass,
                    message = "SessionManager found"
                });

                if (string.IsNullOrEmpty(sessionManager.supabaseUrl))
                {
                    results.Add(new ValidationResult
                    {
                        category = "Managers",
                        status = ValidationStatus.Fail,
                        message = "SessionManager supabaseUrl is not set",
                        details = "Add your Supabase project URL in Inspector"
                    });
                }

                if (string.IsNullOrEmpty(sessionManager.supabaseAnonKey))
                {
                    results.Add(new ValidationResult
                    {
                        category = "Managers",
                        status = ValidationStatus.Fail,
                        message = "SessionManager supabaseAnonKey is not set",
                        details = "Add your Supabase anon key in Inspector"
                    });
                }
            }
            else
            {
                results.Add(new ValidationResult
                {
                    category = "Managers",
                    status = ValidationStatus.Fail,
                    message = "SessionManager not found in scene"
                });
            }

            // InputHandler
            ValidateComponent<InputHandler>("InputHandler");

            // MissionsManager
            ValidateComponent<MissionsManager>("MissionsManager");

            // PracticeMode
            ValidateComponent<PracticeMode>("PracticeMode");

            // AnalyticsManager
            ValidateComponent<AnalyticsManager>("AnalyticsManager");

            // AnalyticsIntegration
            ValidateComponent<AnalyticsIntegration>("AnalyticsIntegration");
        }

        private void ValidateUIScreens()
        {
            // HomeScreen
            var homeScreen = FindObjectOfType<HomeScreen>(true);
            if (homeScreen != null)
            {
                results.Add(new ValidationResult
                {
                    category = "UI",
                    status = ValidationStatus.Pass,
                    message = "HomeScreen found"
                });

                ValidateButton(homeScreen.startButton, "HomeScreen.startButton");
                ValidateButton(homeScreen.practiceButton, "HomeScreen.practiceButton");
                ValidateButton(homeScreen.leaderboardButton, "HomeScreen.leaderboardButton");
                ValidateButton(homeScreen.missionsButton, "HomeScreen.missionsButton");
                ValidateButton(homeScreen.shopButton, "HomeScreen.shopButton");
                ValidateButton(homeScreen.settingsButton, "HomeScreen.settingsButton");
            }
            else
            {
                results.Add(new ValidationResult
                {
                    category = "UI",
                    status = ValidationStatus.Fail,
                    message = "HomeScreen not found"
                });
            }

            // GameScreen
            var gameScreen = FindObjectOfType<GameScreen>(true);
            if (gameScreen != null)
            {
                results.Add(new ValidationResult
                {
                    category = "UI",
                    status = ValidationStatus.Pass,
                    message = "GameScreen found"
                });
            }
            else
            {
                results.Add(new ValidationResult
                {
                    category = "UI",
                    status = ValidationStatus.Fail,
                    message = "GameScreen not found"
                });
            }

            // ResultsScreen
            ValidateComponent<ResultsScreen>("ResultsScreen", true);

            // LeaderboardScreen
            var leaderboardScreen = FindObjectOfType<LeaderboardScreen>(true);
            if (leaderboardScreen != null)
            {
                results.Add(new ValidationResult
                {
                    category = "UI",
                    status = ValidationStatus.Pass,
                    message = "LeaderboardScreen found"
                });

                if (leaderboardScreen.entryPrefab == null)
                {
                    results.Add(new ValidationResult
                    {
                        category = "UI",
                        status = ValidationStatus.Fail,
                        message = "LeaderboardScreen.entryPrefab is not assigned"
                    });
                }
            }
            else
            {
                results.Add(new ValidationResult
                {
                    category = "UI",
                    status = ValidationStatus.Fail,
                    message = "LeaderboardScreen not found"
                });
            }

            // MissionsScreen
            var missionsScreen = FindObjectOfType<MissionsScreen>(true);
            if (missionsScreen != null)
            {
                results.Add(new ValidationResult
                {
                    category = "UI",
                    status = ValidationStatus.Pass,
                    message = "MissionsScreen found"
                });

                if (missionsScreen.missionEntryPrefab == null)
                {
                    results.Add(new ValidationResult
                    {
                        category = "UI",
                        status = ValidationStatus.Fail,
                        message = "MissionsScreen.missionEntryPrefab is not assigned"
                    });
                }
            }
            else
            {
                results.Add(new ValidationResult
                {
                    category = "UI",
                    status = ValidationStatus.Fail,
                    message = "MissionsScreen not found"
                });
            }

            // PracticeScreen
            ValidateComponent<PracticeScreen>("PracticeScreen", true);

            // ShopScreen
            var shopScreen = FindObjectOfType<ShopScreen>(true);
            if (shopScreen != null)
            {
                results.Add(new ValidationResult
                {
                    category = "UI",
                    status = ValidationStatus.Pass,
                    message = "ShopScreen found"
                });

                if (shopScreen.itemPrefab == null)
                {
                    results.Add(new ValidationResult
                    {
                        category = "UI",
                        status = ValidationStatus.Fail,
                        message = "ShopScreen.itemPrefab is not assigned"
                    });
                }
            }
            else
            {
                results.Add(new ValidationResult
                {
                    category = "UI",
                    status = ValidationStatus.Fail,
                    message = "ShopScreen not found"
                });
            }

            // SettingsScreen
            ValidateComponent<SettingsScreen>("SettingsScreen", true);
        }

        private void ValidateCanvas()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                results.Add(new ValidationResult
                {
                    category = "Canvas",
                    status = ValidationStatus.Pass,
                    message = "Canvas found"
                });

                var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (scaler != null)
                {
                    if (scaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize)
                    {
                        results.Add(new ValidationResult
                        {
                            category = "Canvas",
                            status = ValidationStatus.Pass,
                            message = "CanvasScaler is set to Scale With Screen Size"
                        });

                        if (scaler.referenceResolution == new Vector2(1080, 1920))
                        {
                            results.Add(new ValidationResult
                            {
                                category = "Canvas",
                                status = ValidationStatus.Pass,
                                message = "Reference resolution is 1080x1920"
                            });
                        }
                        else
                        {
                            results.Add(new ValidationResult
                            {
                                category = "Canvas",
                                status = ValidationStatus.Warning,
                                message = $"Reference resolution is {scaler.referenceResolution.x}x{scaler.referenceResolution.y} (recommended: 1080x1920)"
                            });
                        }
                    }
                    else
                    {
                        results.Add(new ValidationResult
                        {
                            category = "Canvas",
                            status = ValidationStatus.Warning,
                            message = "CanvasScaler should be set to Scale With Screen Size"
                        });
                    }
                }
                else
                {
                    results.Add(new ValidationResult
                    {
                        category = "Canvas",
                        status = ValidationStatus.Fail,
                        message = "Canvas is missing CanvasScaler component"
                    });
                }
            }
            else
            {
                results.Add(new ValidationResult
                {
                    category = "Canvas",
                    status = ValidationStatus.Fail,
                    message = "No Canvas found in scene"
                });
            }
        }

        private void ValidatePrefabs()
        {
            // Check if required prefabs exist in project
            string[] requiredPrefabs = new string[]
            {
                "LeaderboardEntry",
                "MissionEntry",
                "ShopItem"
            };

            foreach (string prefabName in requiredPrefabs)
            {
                string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");
                if (guids.Length > 0)
                {
                    results.Add(new ValidationResult
                    {
                        category = "Prefabs",
                        status = ValidationStatus.Pass,
                        message = $"{prefabName} prefab exists"
                    });
                }
                else
                {
                    results.Add(new ValidationResult
                    {
                        category = "Prefabs",
                        status = ValidationStatus.Warning,
                        message = $"{prefabName} prefab not found in project",
                        details = "Create this prefab or assign an existing one"
                    });
                }
            }
        }

        private void ValidateAssets()
        {
            // Check for placeholder sprites
            string[] spriteGuids = AssetDatabase.FindAssets("Pattern_ t:Texture2D");
            if (spriteGuids.Length >= 6)
            {
                results.Add(new ValidationResult
                {
                    category = "Assets",
                    status = ValidationStatus.Pass,
                    message = $"Found {spriteGuids.Length} pattern sprites"
                });
            }
            else
            {
                results.Add(new ValidationResult
                {
                    category = "Assets",
                    status = ValidationStatus.Warning,
                    message = "Pattern sprites not found",
                    details = "Run Tools > Tower Climb > Generate Placeholder Sprites"
                });
            }

            // Check for audio clips
            string[] audioGuids = AssetDatabase.FindAssets("Pattern_ t:AudioClip");
            if (audioGuids.Length > 0)
            {
                results.Add(new ValidationResult
                {
                    category = "Assets",
                    status = ValidationStatus.Pass,
                    message = $"Found {audioGuids.Length} audio clips"
                });
            }
            else
            {
                results.Add(new ValidationResult
                {
                    category = "Assets",
                    status = ValidationStatus.Warning,
                    message = "Audio clips not found",
                    details = "Run Tools > Tower Climb > Generate Placeholder Audio"
                });
            }
        }

        private void ValidateComponent<T>(string componentName, bool includeInactive = false) where T : Component
        {
            var component = FindObjectOfType<T>(includeInactive);
            if (component != null)
            {
                results.Add(new ValidationResult
                {
                    category = "Managers",
                    status = ValidationStatus.Pass,
                    message = $"{componentName} found"
                });
            }
            else
            {
                results.Add(new ValidationResult
                {
                    category = "Managers",
                    status = ValidationStatus.Fail,
                    message = $"{componentName} not found in scene"
                });
            }
        }

        private void ValidateButton(UnityEngine.UI.Button button, string fieldName)
        {
            if (button == null)
            {
                results.Add(new ValidationResult
                {
                    category = "UI",
                    status = ValidationStatus.Warning,
                    message = $"{fieldName} is not assigned"
                });
            }
        }

        #endregion
    }

    public class ValidationResult
    {
        public string category;
        public ValidationStatus status;
        public string message;
        public string details;
    }

    public enum ValidationStatus
    {
        Pass,
        Fail,
        Warning
    }
}
#endif
