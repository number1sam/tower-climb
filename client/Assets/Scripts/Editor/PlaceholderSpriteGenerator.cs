#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace TowerClimb.Editor
{
    /// <summary>
    /// Unity Editor tool to generate placeholder sprites for pattern icons
    /// Menu: Tools > Tower Climb > Generate Placeholder Sprites
    /// </summary>
    public class PlaceholderSpriteGenerator : EditorWindow
    {
        private int spriteSize = 128;
        private string outputFolder = "Assets/Sprites/Generated";

        [MenuItem("Tools/Tower Climb/Generate Placeholder Sprites")]
        public static void ShowWindow()
        {
            GetWindow<PlaceholderSpriteGenerator>("Sprite Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Placeholder Sprite Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            spriteSize = EditorGUILayout.IntField("Sprite Size (px)", spriteSize);
            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

            GUILayout.Space(20);

            if (GUILayout.Button("Generate All Pattern Icons", GUILayout.Height(40)))
            {
                GenerateAllPatternIcons();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Generate UI Placeholders", GUILayout.Height(40)))
            {
                GenerateUIPlaceholders();
            }

            GUILayout.Space(20);
            GUILayout.Label("This will create simple geometric shapes for each pattern type.", EditorStyles.helpBox);
        }

        private void GenerateAllPatternIcons()
        {
            EnsureOutputFolderExists();

            // Generate pattern icons
            GeneratePatternIcon("Tap", Color.cyan, DrawTapPattern);
            GeneratePatternIcon("Swipe", Color.yellow, DrawSwipePattern);
            GeneratePatternIcon("Hold", Color.magenta, DrawHoldPattern);
            GeneratePatternIcon("Rhythm", Color.green, DrawRhythmPattern);
            GeneratePatternIcon("Tilt", Color.red, DrawTiltPattern);
            GeneratePatternIcon("DoubleTap", Color.blue, DrawDoubleTapPattern);

            AssetDatabase.Refresh();
            Debug.Log($"[PlaceholderGenerator] Generated 6 pattern icons in {outputFolder}");
            EditorUtility.DisplayDialog("Success", "Generated all pattern icons!", "OK");
        }

        private void GenerateUIPlaceholders()
        {
            EnsureOutputFolderExists();

            // Generate UI elements
            GenerateUIIcon("Button_Background", Color.white, DrawRoundedRect);
            GenerateUIIcon("Panel_Background", new Color(0.1f, 0.1f, 0.1f, 0.9f), DrawPanel);
            GenerateUIIcon("Icon_Star", Color.yellow, DrawStar);
            GenerateUIIcon("Icon_Lock", Color.gray, DrawLock);
            GenerateUIIcon("Icon_CheckMark", Color.green, DrawCheckMark);

            AssetDatabase.Refresh();
            Debug.Log($"[PlaceholderGenerator] Generated 5 UI placeholders in {outputFolder}");
            EditorUtility.DisplayDialog("Success", "Generated UI placeholders!", "OK");
        }

        private void GeneratePatternIcon(string name, Color color, System.Action<Texture2D, Color> drawAction)
        {
            Texture2D texture = new Texture2D(spriteSize, spriteSize, TextureFormat.RGBA32, false);

            // Clear to transparent
            Color[] pixels = new Color[spriteSize * spriteSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            texture.SetPixels(pixels);

            // Draw pattern
            drawAction(texture, color);
            texture.Apply();

            // Save as PNG
            byte[] pngData = texture.EncodeToPNG();
            string path = Path.Combine(outputFolder, $"Pattern_{name}.png");
            File.WriteAllBytes(path, pngData);

            // Import and configure as sprite
            AssetDatabase.ImportAsset(path);
            ConfigureAsSprite(path);
        }

        private void GenerateUIIcon(string name, Color color, System.Action<Texture2D, Color> drawAction)
        {
            Texture2D texture = new Texture2D(spriteSize, spriteSize, TextureFormat.RGBA32, false);

            Color[] pixels = new Color[spriteSize * spriteSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            texture.SetPixels(pixels);

            drawAction(texture, color);
            texture.Apply();

            byte[] pngData = texture.EncodeToPNG();
            string path = Path.Combine(outputFolder, $"{name}.png");
            File.WriteAllBytes(path, pngData);

            AssetDatabase.ImportAsset(path);
            ConfigureAsSprite(path);
        }

        private void ConfigureAsSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        private void EnsureOutputFolderExists()
        {
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
        }

        #region Drawing Functions

        private void DrawTapPattern(Texture2D texture, Color color)
        {
            // Draw a circle with a finger tap indicator
            int centerX = spriteSize / 2;
            int centerY = spriteSize / 2;
            int radius = spriteSize / 3;

            DrawCircle(texture, centerX, centerY, radius, color);
            DrawCircle(texture, centerX, centerY, radius - 4, Color.clear); // Hollow

            // Inner dot
            DrawCircle(texture, centerX, centerY, radius / 4, color);
        }

        private void DrawSwipePattern(Texture2D texture, Color color)
        {
            // Draw an arrow
            int startY = spriteSize / 4;
            int endY = 3 * spriteSize / 4;
            int centerX = spriteSize / 2;

            // Arrow line
            DrawLine(texture, centerX, startY, centerX, endY, 8, color);

            // Arrowhead
            DrawLine(texture, centerX, endY, centerX - 20, endY - 20, 6, color);
            DrawLine(texture, centerX, endY, centerX + 20, endY - 20, 6, color);
        }

        private void DrawHoldPattern(Texture2D texture, Color color)
        {
            // Draw concentric circles
            int centerX = spriteSize / 2;
            int centerY = spriteSize / 2;

            DrawCircle(texture, centerX, centerY, spriteSize / 3, color);
            DrawCircle(texture, centerX, centerY, spriteSize / 3 - 4, Color.clear);
            DrawCircle(texture, centerX, centerY, spriteSize / 4, color);
            DrawCircle(texture, centerX, centerY, spriteSize / 4 - 4, Color.clear);
            DrawCircleFilled(texture, centerX, centerY, spriteSize / 6, color);
        }

        private void DrawRhythmPattern(Texture2D texture, Color color)
        {
            // Draw musical notes
            int spacing = spriteSize / 5;
            for (int i = 1; i <= 3; i++)
            {
                int x = i * spacing;
                int y = spriteSize / 2 + (i % 2 == 0 ? 10 : -10);
                DrawCircleFilled(texture, x, y, 8, color);
                DrawLine(texture, x + 8, y, x + 8, y + 30, 3, color);
            }
        }

        private void DrawTiltPattern(Texture2D texture, Color color)
        {
            // Draw tilted phone icon
            int centerX = spriteSize / 2;
            int centerY = spriteSize / 2;
            int width = spriteSize / 3;
            int height = spriteSize / 2;

            // Tilted rectangle
            DrawRectangle(texture, centerX - width / 2, centerY - height / 2, width, height, color);

            // Tilt indicator
            DrawLine(texture, centerX - 15, centerY - height / 2 - 10, centerX + 15, centerY - height / 2 - 20, 4, color);
        }

        private void DrawDoubleTapPattern(Texture2D texture, Color color)
        {
            // Two overlapping circles
            int centerY = spriteSize / 2;
            int offset = spriteSize / 8;

            DrawCircle(texture, spriteSize / 2 - offset, centerY, spriteSize / 4, color);
            DrawCircle(texture, spriteSize / 2 + offset, centerY, spriteSize / 4, color);

            // Fill
            DrawCircleFilled(texture, spriteSize / 2 - offset, centerY, spriteSize / 5, color);
            DrawCircleFilled(texture, spriteSize / 2 + offset, centerY, spriteSize / 5, color);
        }

        private void DrawRoundedRect(Texture2D texture, Color color)
        {
            int margin = 10;
            DrawRectangle(texture, margin, margin, spriteSize - margin * 2, spriteSize - margin * 2, color);
        }

        private void DrawPanel(Texture2D texture, Color color)
        {
            int margin = 5;
            for (int x = margin; x < spriteSize - margin; x++)
            {
                for (int y = margin; y < spriteSize - margin; y++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        private void DrawStar(Texture2D texture, Color color)
        {
            int centerX = spriteSize / 2;
            int centerY = spriteSize / 2;
            int outerRadius = spriteSize / 3;
            int innerRadius = spriteSize / 6;

            // 5-point star
            Vector2Int[] points = new Vector2Int[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = i * Mathf.PI / 5 - Mathf.PI / 2;
                int radius = (i % 2 == 0) ? outerRadius : innerRadius;
                points[i] = new Vector2Int(
                    centerX + Mathf.RoundToInt(radius * Mathf.Cos(angle)),
                    centerY + Mathf.RoundToInt(radius * Mathf.Sin(angle))
                );
            }

            for (int i = 0; i < 10; i++)
            {
                DrawLine(texture, points[i].x, points[i].y, points[(i + 1) % 10].x, points[(i + 1) % 10].y, 3, color);
            }
        }

        private void DrawLock(Texture2D texture, Color color)
        {
            int centerX = spriteSize / 2;
            int lockY = spriteSize / 2 + 10;
            int lockWidth = spriteSize / 3;
            int lockHeight = spriteSize / 4;

            // Lock body
            DrawRectangle(texture, centerX - lockWidth / 2, lockY - lockHeight / 2, lockWidth, lockHeight, color);

            // Lock shackle
            DrawCircle(texture, centerX, lockY - lockHeight / 2, lockWidth / 3, color);
        }

        private void DrawCheckMark(Texture2D texture, Color color)
        {
            int centerX = spriteSize / 2;
            int centerY = spriteSize / 2;

            // Checkmark lines
            DrawLine(texture, centerX - 20, centerY, centerX - 5, centerY + 15, 6, color);
            DrawLine(texture, centerX - 5, centerY + 15, centerX + 25, centerY - 20, 6, color);
        }

        #endregion

        #region Primitive Drawing

        private void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            for (int angle = 0; angle < 360; angle++)
            {
                float rad = angle * Mathf.Deg2Rad;
                int x = centerX + Mathf.RoundToInt(radius * Mathf.Cos(rad));
                int y = centerY + Mathf.RoundToInt(radius * Mathf.Sin(rad));

                if (x >= 0 && x < spriteSize && y >= 0 && y < spriteSize)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        private void DrawCircleFilled(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        int px = centerX + x;
                        int py = centerY + y;
                        if (px >= 0 && px < spriteSize && py >= 0 && py < spriteSize)
                        {
                            texture.SetPixel(px, py, color);
                        }
                    }
                }
            }
        }

        private void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, int thickness, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                // Draw thick line
                for (int ty = -thickness / 2; ty <= thickness / 2; ty++)
                {
                    for (int tx = -thickness / 2; tx <= thickness / 2; tx++)
                    {
                        int px = x0 + tx;
                        int py = y0 + ty;
                        if (px >= 0 && px < spriteSize && py >= 0 && py < spriteSize)
                        {
                            texture.SetPixel(px, py, color);
                        }
                    }
                }

                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private void DrawRectangle(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int px = x + i;
                    int py = y + j;
                    if (px >= 0 && px < spriteSize && py >= 0 && py < spriteSize)
                    {
                        // Only draw border
                        if (i == 0 || i == width - 1 || j == 0 || j == height - 1)
                        {
                            texture.SetPixel(px, py, color);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
#endif
