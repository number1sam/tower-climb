#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace TowerClimb.Editor
{
    /// <summary>
    /// Unity Editor tool to generate placeholder audio clips
    /// Menu: Tools > Tower Climb > Generate Placeholder Audio
    /// </summary>
    public class PlaceholderAudioGenerator : EditorWindow
    {
        private int sampleRate = 44100;
        private string outputFolder = "Assets/Audio/Generated";

        [MenuItem("Tools/Tower Climb/Generate Placeholder Audio")]
        public static void ShowWindow()
        {
            GetWindow<PlaceholderAudioGenerator>("Audio Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Placeholder Audio Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            sampleRate = EditorGUILayout.IntField("Sample Rate", sampleRate);
            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

            GUILayout.Space(20);

            if (GUILayout.Button("Generate Pattern SFX", GUILayout.Height(40)))
            {
                GeneratePatternSFX();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Generate UI SFX", GUILayout.Height(40)))
            {
                GenerateUISFX();
            }

            GUILayout.Space(20);
            GUILayout.Label("This will create simple sine wave tones for each sound effect.", EditorStyles.helpBox);
        }

        private void GeneratePatternSFX()
        {
            EnsureOutputFolderExists();

            // Pattern-specific sounds with different frequencies
            GenerateTone("Pattern_Tap", 800f, 0.1f, 0.3f);
            GenerateTone("Pattern_Swipe", 600f, 0.15f, 0.3f);
            GenerateTone("Pattern_Hold", 400f, 0.4f, 0.25f);
            GenerateChord("Pattern_Rhythm", new float[] { 500f, 700f, 900f }, 0.2f, 0.3f);
            GenerateTone("Pattern_Tilt", 500f, 0.12f, 0.3f);
            GenerateDoubleTone("Pattern_DoubleTap", 900f, 0.08f, 0.05f, 0.3f);

            // Success/fail sounds
            GenerateChord("Pattern_Success", new float[] { 600f, 800f, 1000f }, 0.25f, 0.4f);
            GenerateTone("Pattern_Fail", 200f, 0.3f, 0.5f);
            GenerateTone("Pattern_Perfect", 1200f, 0.2f, 0.5f);

            AssetDatabase.Refresh();
            Debug.Log($"[PlaceholderGenerator] Generated 9 pattern SFX in {outputFolder}");
            EditorUtility.DisplayDialog("Success", "Generated pattern SFX!", "OK");
        }

        private void GenerateUISFX()
        {
            EnsureOutputFolderExists();

            // UI sounds
            GenerateTone("UI_Click", 1000f, 0.05f, 0.3f);
            GenerateTone("UI_Back", 600f, 0.08f, 0.25f);
            GenerateChord("UI_Unlock", new float[] { 800f, 1000f, 1200f }, 0.3f, 0.4f);
            GenerateTone("UI_Error", 300f, 0.2f, 0.4f);
            GenerateChord("UI_Success", new float[] { 700f, 900f, 1100f }, 0.25f, 0.45f);

            AssetDatabase.Refresh();
            Debug.Log($"[PlaceholderGenerator] Generated 5 UI SFX in {outputFolder}");
            EditorUtility.DisplayDialog("Success", "Generated UI SFX!", "OK");
        }

        private void GenerateTone(string name, float frequency, float duration, float volume)
        {
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = CalculateEnvelope(t, duration);
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * volume * envelope;
            }

            SaveAudioClip(name, samples, sampleRate);
        }

        private void GenerateChord(string name, float[] frequencies, float duration, float volume)
        {
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = CalculateEnvelope(t, duration);
                float sample = 0f;

                // Add all frequencies together
                foreach (float freq in frequencies)
                {
                    sample += Mathf.Sin(2 * Mathf.PI * freq * t);
                }

                // Average and apply volume
                samples[i] = (sample / frequencies.Length) * volume * envelope;
            }

            SaveAudioClip(name, samples, sampleRate);
        }

        private void GenerateDoubleTone(string name, float frequency, float tone1Duration, float gapDuration, float volume)
        {
            float totalDuration = tone1Duration * 2 + gapDuration;
            int sampleCount = Mathf.RoundToInt(sampleRate * totalDuration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;

                // First tone
                if (t < tone1Duration)
                {
                    float envelope = CalculateEnvelope(t, tone1Duration);
                    samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * volume * envelope;
                }
                // Gap (silence)
                else if (t < tone1Duration + gapDuration)
                {
                    samples[i] = 0f;
                }
                // Second tone
                else
                {
                    float t2 = t - (tone1Duration + gapDuration);
                    float envelope = CalculateEnvelope(t2, tone1Duration);
                    samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * volume * envelope;
                }
            }

            SaveAudioClip(name, samples, sampleRate);
        }

        private float CalculateEnvelope(float t, float duration)
        {
            // ADSR envelope (simplified)
            float attack = 0.01f;
            float decay = 0.05f;
            float sustain = 0.7f;
            float release = 0.1f;

            if (t < attack)
            {
                // Attack: ramp up
                return t / attack;
            }
            else if (t < attack + decay)
            {
                // Decay: ramp down to sustain level
                float decayProgress = (t - attack) / decay;
                return 1f - (1f - sustain) * decayProgress;
            }
            else if (t < duration - release)
            {
                // Sustain: hold at sustain level
                return sustain;
            }
            else
            {
                // Release: ramp down to zero
                float releaseProgress = (t - (duration - release)) / release;
                return sustain * (1f - releaseProgress);
            }
        }

        private void SaveAudioClip(string name, float[] samples, int sampleRate)
        {
            string path = Path.Combine(outputFolder, $"{name}.wav");

            // Create WAV file
            using (FileStream fileStream = File.Create(path))
            {
                WriteWAVHeader(fileStream, samples.Length, sampleRate);

                // Write audio data
                foreach (float sample in samples)
                {
                    short value = (short)(sample * short.MaxValue);
                    byte[] bytes = System.BitConverter.GetBytes(value);
                    fileStream.Write(bytes, 0, bytes.Length);
                }
            }

            AssetDatabase.ImportAsset(path);
            ConfigureAsAudioClip(path);
        }

        private void WriteWAVHeader(FileStream stream, int sampleCount, int sampleRate)
        {
            int channels = 1; // Mono
            int bitsPerSample = 16;
            int byteRate = sampleRate * channels * (bitsPerSample / 8);
            int blockAlign = channels * (bitsPerSample / 8);
            int dataSize = sampleCount * blockAlign;

            // RIFF header
            stream.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"), 0, 4);
            stream.Write(System.BitConverter.GetBytes(36 + dataSize), 0, 4);
            stream.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"), 0, 4);

            // fmt chunk
            stream.Write(System.Text.Encoding.ASCII.GetBytes("fmt "), 0, 4);
            stream.Write(System.BitConverter.GetBytes(16), 0, 4); // Chunk size
            stream.Write(System.BitConverter.GetBytes((short)1), 0, 2); // Audio format (PCM)
            stream.Write(System.BitConverter.GetBytes((short)channels), 0, 2);
            stream.Write(System.BitConverter.GetBytes(sampleRate), 0, 4);
            stream.Write(System.BitConverter.GetBytes(byteRate), 0, 4);
            stream.Write(System.BitConverter.GetBytes((short)blockAlign), 0, 2);
            stream.Write(System.BitConverter.GetBytes((short)bitsPerSample), 0, 2);

            // data chunk
            stream.Write(System.Text.Encoding.ASCII.GetBytes("data"), 0, 4);
            stream.Write(System.BitConverter.GetBytes(dataSize), 0, 4);
        }

        private void ConfigureAsAudioClip(string path)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer != null)
            {
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                importer.defaultSampleSettings = settings;
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
    }
}
#endif
