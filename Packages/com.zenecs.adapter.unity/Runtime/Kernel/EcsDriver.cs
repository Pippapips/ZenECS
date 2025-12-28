// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity
// File: EcsDriver.cs
// Purpose: Unity-facing MonoBehaviour entry point that owns a single ZenECS
//          kernel instance and bridges Unity's frame callbacks into it.
// Key concepts:
//   • MonoBehaviour host: creates and stores a single IKernel instance.
//   • Singleton-style driver: destroys duplicate drivers in the same scene.
//   • KernelLocator integration: attaches/detaches the kernel globally.
//   • Frame bridge: forwards Update/FixedUpdate/LateUpdate into the kernel.
//   • Editor safety: hides the component to avoid accidental editing/saving.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Config;
using ZenECS.Adapter.Unity.Linking;

namespace ZenECS.Adapter.Unity
{
    /// <summary>
    /// Unity <see cref="MonoBehaviour"/> entry point that owns a single
    /// <see cref="IKernel"/> instance and drives its frame lifecycle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This component is intended to be placed once in a scene (for example on
    /// a bootstrap or systems GameObject). On <see cref="Awake"/>, it:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Ensures only one <see cref="EcsDriver"/> exists in the scene.</description></item>
    /// <item><description>Creates a <see cref="Kernel"/> instance if none exists.</description></item>
    /// <item><description>Attaches the kernel to <see cref="KernelLocator"/> for global access.</description></item>
    /// <item><description>Forwards Unity's frame callbacks into the kernel.</description></item>
    /// </list>
    /// <para>
    /// The driver does not configure any worlds by itself; game code is expected
    /// to create and configure worlds via the kernel once it is available.
    /// </para>
    /// </remarks>
    [DefaultExecutionOrder(-32000)]
    [DisallowMultipleComponent]
    sealed class EcsDriver : MonoBehaviour
    {
        /// <summary>
        /// Gets the kernel instance owned by this driver.
        /// </summary>
        /// <remarks>
        /// The value is <c>null</c> until <see cref="CreateKernel"/> has been
        /// called (which normally happens during <see cref="Awake"/>).
        /// </remarks>
        public IKernel? Kernel { get; private set; }

        /// <summary>
        /// Creates the kernel instance if it does not already exist.
        /// </summary>
        /// <param name="options">
        /// Optional kernel configuration. When omitted, a default
        /// <see cref="KernelOptions"/> instance is created with
        /// <see cref="KernelOptions.AutoSelectNewWorld"/> disabled so that game
        /// code has explicit control over world selection.
        /// </param>
        /// <returns>
        /// The existing kernel if it was already created; otherwise the newly
        /// created kernel instance.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Constructs a new <see cref="Kernel"/> instance.</description></item>
        /// <item><description>Plugs in a Unity-facing <see cref="Logger"/> for ECS logging.</description></item>
        /// <item><description>Attaches the instance to <see cref="KernelLocator"/>.</description></item>
        /// </list>
        /// <para>
        /// Subsequent calls are idempotent and simply return the already
        /// created kernel.
        /// </para>
        /// </remarks>
        public IKernel CreateKernel(KernelOptions? options = null)
        {
            if (Kernel != null) return Kernel;

            Kernel = new ZenECS.Core.Kernel(
                options ?? new KernelOptions
                {
                    // Keep world selection under explicit control of game code.
                    AutoSelectNewWorld = false,
                    StepOnlyCurrentWhenSelected = false,
                },
                new Logger());

            // Subscribe to world destruction events to clean up EntityViewRegistry
            // Unsubscribe first to prevent duplicate subscriptions (defensive coding)
            Kernel.WorldDestroyed -= OnWorldDestroyed;
            Kernel.WorldDestroyed += OnWorldDestroyed;

            KernelLocator.Attach(Kernel);
            ZenEcsUnityBridge.Kernel = Kernel;
            return Kernel;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity editor validation hook used to adjust hide flags.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Marks this component as non-editable and prevents it from being
        /// serialized into scenes or prefabs, reducing the chance of accidental
        /// modifications in the inspector.
        /// </para>
        /// <para>
        /// This method is only compiled and executed inside the Unity Editor.
        /// At runtime, the hide flags are left untouched.
        /// </para>
        /// </remarks>
        private void OnValidate()
        {
            // Make the component non-editable in the Inspector and avoid saving it in scenes.
            hideFlags |= HideFlags.NotEditable;
            hideFlags |= HideFlags.HideAndDontSave;
        }
#endif

        /// <summary>
        /// Unity lifecycle callback invoked when the component is first loaded.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method enforces a single <see cref="EcsDriver"/> instance per
        /// scene by:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Searching for an existing driver instance.</description></item>
        /// <item><description>
        /// Destroying the newer one if a duplicate is found (based on Unity's
        /// object creation order).
        /// </description></item>
        /// <item><description>
        /// Creating and attaching the kernel if this driver is the primary one.
        /// </description></item>
        /// </list>
        /// </remarks>
        private void Awake()
        {
#if UNITY_2022_2_OR_NEWER
            var first = Object.FindFirstObjectByType<EcsDriver>(FindObjectsInactive.Include);
#else
            var first = Object.FindObjectOfType<EcsDriver>(true);
#endif
            if (first != null && first != this)
            {
                Debug.LogWarning("[EcsDriver] Duplicate found. Destroying the newer one.");

                if (Application.isPlaying)
                    Object.Destroy(gameObject);
                else
                    Object.DestroyImmediate(gameObject);

                return;
            }

            CreateKernel();
        }

        /// <summary>
        /// Handles world destruction events to clean up EntityViewRegistry.
        /// </summary>
        /// <param name="world">The world that was destroyed.</param>
        private void OnWorldDestroyed(IWorld world)
        {
            EntityViewRegistry.CleanupWorld(world);
        }

        /// <summary>
        /// Unity lifecycle callback invoked when the component or its GameObject
        /// is being destroyed.
        /// </summary>
        /// <remarks>
        /// If a kernel instance is owned by this driver, it is:
        /// <list type="bullet">
        /// <item><description>Unsubscribed from world destruction events.</description></item>
        /// <item><description>Detached from <see cref="KernelLocator"/>.</description></item>
        /// <item><description>Disposed to release any managed resources.</description></item>
        /// <item><description>Cleared from <see cref="Kernel"/> to avoid reuse.</description></item>
        /// </list>
        /// </remarks>
        private void OnDestroy()
        {
            if (Kernel == null) return;

            // Unsubscribe from events before disposing
            Kernel.WorldDestroyed -= OnWorldDestroyed;

            KernelLocator.Detach(Kernel);
            Kernel.Dispose();
            ZenEcsUnityBridge.Clear();
            Kernel = null;
        }

        /// <summary>
        /// Unity frame callback used to drive the kernel's variable-timestep
        /// update phase.
        /// </summary>
        /// <remarks>
        /// Forwards <see cref="Time.deltaTime"/> to
        /// <see cref="IKernel.BeginFrame(float)"/> when a kernel is present.
        /// </remarks>
        private void Update()      => Kernel?.BeginFrame(Time.deltaTime);

        /// <summary>
        /// Unity fixed-timestep callback used to drive the kernel's
        /// deterministic simulation step.
        /// </summary>
        /// <remarks>
        /// Forwards <see cref="Time.fixedDeltaTime"/> to
        /// <see cref="IKernel.FixedStep(float)"/> when a kernel is present.
        /// </remarks>
        private void FixedUpdate() => Kernel?.FixedStep(Time.fixedDeltaTime);

        /// <summary>
        /// Unity late-frame callback used to drive the kernel's presentation
        /// phase.
        /// </summary>
        /// <remarks>
        /// Forwards to <see cref="IKernel.LateFrame(float)"/> with the default
        /// interpolation alpha (1.0f) when a kernel is present. The kernel
        /// internally calculates the interpolation alpha based on the fixed-step
        /// accumulator, so the default value is appropriate for Unity's LateUpdate.
        /// </remarks>
        private void LateUpdate()  => Kernel?.LateFrame();
    }

    /// <summary>
    /// Simple logger that routes ECS log messages to Unity's
    /// <see cref="Debug"/> logging system.
    /// </summary>
    /// <remarks>
    /// This implementation is used as the default <see cref="IEcsLogger"/>
    /// for <see cref="EcsDriver"/> so that the ECS runtime integrates with
    /// the familiar Unity Console output.
    /// </remarks>
    sealed class Logger : IEcsLogger
    {
        /// <summary>
        /// Logs an informational message to the Unity Console.
        /// </summary>
        /// <param name="m">Message text to log.</param>
        public void Info(string m)  => Debug.Log(m);

        /// <summary>
        /// Logs a warning message to the Unity Console.
        /// </summary>
        /// <param name="m">Message text to log.</param>
        public void Warn(string m)  => Debug.LogWarning(m);

        /// <summary>
        /// Logs an error message to the Unity Console.
        /// </summary>
        /// <param name="m">Message text to log.</param>
        public void Error(string m) => Debug.LogError(m);
    }
}
