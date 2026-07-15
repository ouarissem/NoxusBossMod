using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBossMod.Core.Graphics
{
    public class RenderTargetManager : ModSystem
    {
        internal static List<ManagedRenderTarget> ManagedTargets = new();

        public delegate void RenderTargetUpdateDelegate();

        public static event RenderTargetUpdateDelegate RenderTargetUpdateLoopEvent;

        private static Hook _setDisplayModeHook;

        private delegate void orig_SetDisplayMode(int width, int height, bool fullscreen);

        private void SetDisplayModeDetour(orig_SetDisplayMode orig, int width, int height, bool fullscreen)
        {
            foreach (ManagedRenderTarget target in ManagedTargets)
            {
                if (target is null || !target.IsDisposed || target.WaitingForFirstInitialization)
                    continue;

                Main.QueueMainThreadAction(() => target.Recreate(width, height));
            }

            orig(width, height, fullscreen);
        }

        internal static void DisposeOfTargets()
        {
            if (ManagedTargets is null)
                return;

            Main.QueueMainThreadAction(() =>
            {
                foreach (ManagedRenderTarget target in ManagedTargets)
                    target?.Dispose();
                ManagedTargets.Clear();
            });
        }

        public static RenderTarget2D CreateScreenSizedTarget(int screenWidth, int screenHeight) =>
            new(Main.instance.GraphicsDevice, screenWidth, screenHeight, true, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.PreserveContents);

        public override void Load()
        {
            ManagedTargets = new();
            Main.OnPreDraw += HandleTargetUpdateLoop;

            try
            {
                MethodInfo setDisplayModeMethod = typeof(Main).GetMethod("SetDisplayMode", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                if (setDisplayModeMethod != null)
                {
                    _setDisplayModeHook = new Hook(setDisplayModeMethod, SetDisplayModeDetour);
                }
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<NoxusBossMod>().Logger.Warn($"Failed to hook SetDisplayMode: {ex.Message}");
            }
        }

        public override void Unload()
        {
            Main.OnPreDraw -= HandleTargetUpdateLoop;
            _setDisplayModeHook?.Dispose();
            DisposeOfTargets();
        }

        private void HandleTargetUpdateLoop(GameTime obj) => RenderTargetUpdateLoopEvent?.Invoke();
    }
}