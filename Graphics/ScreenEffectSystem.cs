using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace NoxusBossMod.Core.Graphics
{
    public class ScreenEffectSystem : ModSystem
    {
        #region Blur
        private static RenderTarget2D BlurRenderTarget;
        private static Vector2 BlurPosition;
        private static float BlurIntensity;
        private static int BlurLifeTime;
        private static int BlurTime;
        private static bool BlurActive;

        public static float BaseScaleAmount => NoxusBossConfig.Instance.VisualOverlayIntensity * 0.08f;
        private const float BaseBlurAmount = 4f;
        private static float BlurLifetimeRatio => (float)BlurTime / BlurLifeTime;

        public static void SetBlurEffect(Vector2 position, float intensity, int lifetime)
        {
            if (NoxusBossConfig.Instance.VisualOverlayIntensity <= 0f)
                return;

            BlurPosition = position;
            BlurIntensity = intensity;
            BlurLifeTime = lifetime;
            BlurTime = 0;
            BlurActive = true;
        }
        #endregion

        #region Flash
        private static RenderTarget2D FlashRenderTarget;
        private static Vector2 FlashPosition;
        private static float FlashIntensity;
        private static int FlashLifeTime;
        private static int FlashTime;
        private static bool FlashActive;
        private static float FlashLifetimeRatio => (float)FlashTime / FlashLifeTime;

        public static void SetFlashEffect(Vector2 position, float intensity, int lifetime)
        {
            if (NoxusBossConfig.Instance.VisualOverlayIntensity <= 0f)
                return;

            FlashPosition = position;
            FlashIntensity = intensity * NoxusBossConfig.Instance.VisualOverlayIntensity;
            FlashLifeTime = lifetime;
            FlashTime = 0;
            FlashActive = true;
        }
        #endregion

        #region Chromatic Aberration
        private static RenderTarget2D AberrationTarget;
        private static Vector2 AberrationPosition;
        private static float AberrationIntensity;
        private static int AberrationLifeTime;
        private static int AberrationTime;
        private static float AberrationLifetimeRatio => (float)AberrationTime / AberrationLifeTime;

        public static void SetChromaticAberrationEffect(Vector2 position, float intensity, int lifetime)
        {
            if (AberrationLifetimeRatio > 0f || NoxusBossConfig.Instance.VisualOverlayIntensity <= 0f)
                return;

            AberrationPosition = position;
            AberrationIntensity = intensity * NoxusBossConfig.Instance.VisualOverlayIntensity;
            AberrationLifeTime = lifetime;
            AberrationTime = 1;
        }
        #endregion

        public override void OnModLoad()
        {
            Main.OnResolutionChanged += ResizeRenderTarget;
          // On_FilterManager.EndCapture += EndCaptureManager;
        }

        public override void OnModUnload()
        {
            Main.OnResolutionChanged -= ResizeRenderTarget;
           // On_FilterManager.EndCapture -= EndCaptureManager;
            Main.QueueMainThreadAction(() =>
            {
                FlashRenderTarget?.Dispose();
                BlurRenderTarget?.Dispose();
                AberrationTarget?.Dispose();
            });
        }

        private void EndCaptureManager(On_FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            screenTarget1 = DrawBlurEffect(screenTarget1);
            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
            //ScreenShatterSystem.CreateSnapshotIfNecessary(screenTarget1);
        }

        public static bool AnyBlurOrFlashActive() => BlurActive || FlashActive;

        public override void PostUpdateEverything()
        {
            if (BlurActive)
            {
                if (BlurTime >= BlurLifeTime)
                {
                    BlurActive = false;
                    BlurTime = 0;
                }
                else
                    BlurTime++;
            }

            if (FlashActive)
            {
                if (FlashTime >= FlashLifeTime)
                {
                    FlashActive = false;
                    FlashTime = 0;
                }
                else
                    FlashTime++;
            }
        }

        internal static RenderTarget2D DrawBlurEffect(RenderTarget2D screenTarget1)
        {
            if (BlurActive)
            {
                if (BlurRenderTarget is null || BlurRenderTarget.IsDisposed)
                    ResizeRenderTarget(Vector2.Zero);

                BlurRenderTarget.SwapToRenderTarget();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                Main.spriteBatch.Draw(screenTarget1, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, 0, 0f);
                Main.spriteBatch.End();

                screenTarget1.SwapToRenderTarget();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

                for (int i = -3; i <= 3; i++)
                {
                    if (i == 0)
                        continue;

                    float scaleAmount = BaseScaleAmount * BlurIntensity;
                    float blurAmount = BaseBlurAmount * BlurIntensity;
                    float scale = 1f + scaleAmount * (1f - BlurLifetimeRatio) * i / blurAmount;
                    Color drawColor = Color.White * 0.42f;
                    Rectangle frameOffset = new(-100, -100, Main.screenWidth + 200, Main.screenHeight + 200);
                    Vector2 origin = BlurPosition + new Vector2(100) - Main.screenPosition;
                    Main.spriteBatch.Draw(BlurRenderTarget, BlurPosition - Main.screenPosition, frameOffset, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
                }

                Main.spriteBatch.End();
            }
            else if (FlashActive)
            {
                if (FlashRenderTarget is null || FlashRenderTarget.IsDisposed)
                    ResizeRenderTarget(Vector2.Zero);

                FlashRenderTarget.SwapToRenderTarget();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                Main.spriteBatch.Draw(screenTarget1, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, 0, 0f);
                Main.spriteBatch.End();

                screenTarget1.SwapToRenderTarget();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

                Color drawColor = new(1f, 1f, 1f, Clamp(Lerp(0.5f, 1f, (1f - FlashLifetimeRatio) * FlashIntensity), 0f, 1f));

                Rectangle frameOffset = new(-100, -100, Main.screenWidth + 200, Main.screenHeight + 200);
                Vector2 origin = FlashPosition + new Vector2(100) - Main.screenPosition;
                for (int i = 0; i < 2; i++)
                    Main.spriteBatch.Draw(FlashRenderTarget, FlashPosition - Main.screenPosition, frameOffset, drawColor, 0f, origin, 1f, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
            }

            if (AberrationLifetimeRatio > 0f)
            {
                if (AberrationTarget is null || AberrationTarget.IsDisposed)
                    ResizeRenderTarget(Vector2.Zero);

                AberrationTime++;
                if (AberrationLifetimeRatio >= 1f)
                {
                    AberrationTime = 0;
                    return screenTarget1;
                }

                AberrationTarget.SwapToRenderTarget();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                Main.spriteBatch.Draw(screenTarget1, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, 0, 0f);
                Main.spriteBatch.End();

                screenTarget1.SwapToRenderTarget();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

                var aberrationShader = GameShaders.Misc[$"{NoxusBossMod.Instance.Name}:ChromaticAberrationShader"];
                aberrationShader.Shader.Parameters["uIntensity"].SetValue((1f - AberrationLifetimeRatio) * AberrationIntensity);
                aberrationShader.Shader.Parameters["impactPoint"].SetValue(AberrationPosition / new Vector2(Main.screenWidth, Main.screenHeight));
                aberrationShader.Apply();

                Main.spriteBatch.Draw(AberrationTarget, Vector2.Zero, Color.White);
                Main.spriteBatch.End();
            }

            return screenTarget1;
        }

        private static void ResizeRenderTarget(Vector2 obj)
        {
            BlurRenderTarget?.Dispose();
            FlashRenderTarget?.Dispose();
            AberrationTarget?.Dispose();

            BlurRenderTarget = new(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight);
            FlashRenderTarget = new(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight);
            AberrationTarget = new(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight);
        }
    }
}