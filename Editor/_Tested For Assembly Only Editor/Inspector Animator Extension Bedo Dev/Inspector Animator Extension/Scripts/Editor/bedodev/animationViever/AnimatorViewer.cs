using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace bedodev.animationViever
{
    [CustomEditor(typeof(Animator))]
    public class AnimatorViewer : Editor
    {
        private int selectedClipIndex = 0;
        private AnimationClip selectedClip;
        private float currentTime;
        private float totalFrames;
        private bool isPlaying = false;
        private bool isPaused = false;
        private float pausedTime = 0f;
        private float currentFrameSliderValue = 0f;


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginVertical();

            Animator animator = (Animator)target;
            RuntimeAnimatorController runtimeController = animator.runtimeAnimatorController;
            
            if (runtimeController != null)
            {
                if (runtimeController is AnimatorController)
                {
                    AnimatorController animatorController = runtimeController as AnimatorController;
                    AnimationClip[] clips = animatorController.animationClips;
                    if (Application.isPlaying)
                    {
                        if (clips.Length > 0)
                        {
                            string[] clipNames = new string[clips.Length];
                            for (int i = 0; i < clips.Length; i++)
                            {
                                clipNames[i] = clips[i].name;
                            }

                            EditorGUILayout.Space();
                            int prevClipIndex = selectedClipIndex;
                            selectedClipIndex =
                                EditorGUILayout.Popup("Select Animation Clip", selectedClipIndex, clipNames);

                            if (prevClipIndex != selectedClipIndex)
                            {
                                if (selectedClipIndex >= 0 && selectedClipIndex < clips.Length)
                                {
                                    selectedClip = clips[selectedClipIndex];
                                    ResetAnimationState(animator);
                                }
                                else
                                {
                                    Debug.LogError("Invalid clip index selected.");
                                }
                            }

                            selectedClip = clips[selectedClipIndex];

                            currentTime = 0f;
                            pausedTime = 0f;
                        }

                        else
                        {
                            EditorGUILayout.HelpBox("No animation clips found in the Animator Controller.",
                                MessageType.Warning);
                            selectedClip = null;
                        }
                    }
                }
                else
                {
                    selectedClip = null;
                    EditorGUILayout.HelpBox("Runtime animator controller is not an Animator Controller.", MessageType.Warning);
                }

                if (selectedClip != null)
                {
                    EditorGUILayout.LabelField("Animation: " + selectedClip.name);
                    totalFrames = selectedClip.length * selectedClip.frameRate;
                    int currentFrame = GetCurrentFrame(animator, selectedClip);
                    EditorGUILayout.LabelField("Current Frame: " + currentFrame + "/" + totalFrames);

                    if (Application.isPlaying) 
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (!isPlaying)
                        {
                            if (GUILayout.Button("Play Animation"))
                            {
                                animator.speed = 1f;
                                isPlaying = true;
                                isPaused = false;
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Pause Animation"))
                            {
                                isPaused = true;
                                isPlaying = false;
                                pausedTime = GetCurrentTime();
                                animator.speed = 0f;
                            }
                        }

                        if (GUILayout.Button("Reset Animation"))
                        {
                            ResetAnimationState(animator);
                        }
                        EditorGUILayout.EndHorizontal();

                        if (isPaused)
                        {
                            EditorGUILayout.BeginHorizontal();
                            if (GUILayout.Button("Previous Frame"))
                            {
                                DecrementFrame(animator, selectedClip);
                            }
                            if (GUILayout.Button("Next Frame"))
                            {
                                IncrementFrame(animator, selectedClip);
                            }
                            EditorGUILayout.EndHorizontal();

             
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Frame Slider", GUILayout.Width(75));
                            currentFrameSliderValue = EditorGUILayout.Slider((int)currentFrameSliderValue, 0f, (int)totalFrames, GUILayout.ExpandWidth(true));
                            if (GUILayout.Button("Jump to Frame"))
                            {
                                JumpToFrame(animator, selectedClip, currentFrameSliderValue);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), (float)currentFrame / totalFrames, "Animation Progress");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No runtime animator controller found.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private int GetCurrentFrame(Animator animator, AnimationClip clip)
        {
            float normalizedTime = Mathf.Clamp01(animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            return Mathf.RoundToInt(normalizedTime * clip.frameRate * clip.length);
        }

        private void IncrementFrame(Animator animator, AnimationClip clip)
        {
            float normalizedTime = Mathf.Clamp01((float)(GetCurrentFrame(animator, clip) + 1) / (clip.frameRate * clip.length));
            animator.Play(selectedClip.name, 0, normalizedTime);
            Repaint();
        }

        private void DecrementFrame(Animator animator, AnimationClip clip)
        {
            float normalizedTime = Mathf.Clamp01((float)(GetCurrentFrame(animator, clip) - 1) / (clip.frameRate * clip.length));
            animator.Play(selectedClip.name, 0, normalizedTime);
            Repaint();
        }

        private void JumpToFrame(Animator animator, AnimationClip clip, float frame)
        {
            float normalizedTime = Mathf.Clamp01(frame / (clip.frameRate * clip.length));
            animator.Play(selectedClip.name, 0, normalizedTime);
            Repaint();
        }
        
        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!isPaused && isPlaying && selectedClip != null)
            {
                float newTime = GetCurrentTime();
                if (currentTime != newTime)
                {
                    currentTime = newTime;
                    Repaint();
                }
            }
        }

        private float GetCurrentTime()
        {
            if (Selection.activeGameObject == null || selectedClip == null)
                return 0f;

            Animator animator = Selection.activeGameObject.GetComponent<Animator>();
            if (animator != null)
            {
                float normalizedTime = Mathf.Clamp01(animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
                return normalizedTime * selectedClip.length;
            }
            return 0f;
        }

        private void ResetAnimationState(Animator animator)
        {
            isPaused = true;
            isPlaying = false;
            animator.Play(selectedClip.name, 0, 0);
            animator.speed = 0f;
        }
    }
}

