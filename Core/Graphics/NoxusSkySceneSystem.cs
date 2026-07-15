using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace NoxusBossMod.Core.Graphics
{
    public class NoxusSkySceneSystem : ModSystem
    {
        internal static Main.SceneArea PreviousSceneDetails { get; private set; }
        public static float EclipseDarknessInterpolant { get; set; }

        public static Vector2 GetSunPosition(Main.SceneArea sceneArea, float dayCompletion)
        {
            float verticalOffsetInterpolant;
            if (dayCompletion < 0.5f)
                verticalOffsetInterpolant = Pow(1f - dayCompletion * 2f, 2f);
            else
                verticalOffsetInterpolant = Pow(dayCompletion - 0.5f, 2f) * 4f;
            Texture2D sunTexture = TextureAssets.Sun.Value;
            int x = (int)(dayCompletion * sceneArea.totalWidth + sunTexture.Width * 2f + dayCompletion * 210f) - 325;
            int y = (int)(sceneArea.bgTopY + verticalOffsetInterpolant * 250f + Main.sunModY + 180f);
            return new(x, y);
        }

        public void DrawNoxusInBackground(Main.SceneArea sceneArea)
        {
            // Only update EclipseDarknessInterpolant without drawing to prevent flickering
            EclipseDarknessInterpolant = Clamp(EclipseDarknessInterpolant - 0.04f, 0f, 1f);

            // Check conditions for drawing Noxus in background
            if (WorldSaveSystem.HasDefeatedEgg || NoxusEggCutsceneSystem.HasSummonedNoxus || !NoxusEggCutsceneSystem.NoxusBeganOrbitingPlanet)
                return;

            if (CelestialOrbitDetails.NoxusOrbitOffset.Z < 0f || Main.gameMenu)
                return;

            // Store original state to restore later
            var originalRenderTarget = Main.instance.GraphicsDevice.GetRenderTargets();

            Texture2D noxusEggTexture = ModContent.Request<Texture2D>("NoxusBossMod/Content/Bosses/NoxusEgg").Value;
            Color noxusDrawColor = Color.Lerp(Color.Black * 0.035f, new Color(64, 64, 64), Pow(EclipseDarknessInterpolant, 0.54f));

            Vector2 noxusDrawPosition = new Vector2(CelestialOrbitDetails.NoxusHorizontalOffset, CelestialOrbitDetails.NoxusVerticalOffset) + sceneArea.SceneLocalScreenPositionOffset;
            noxusDrawPosition.Y += sceneArea.bgTopY;

            Vector2 sunPosition = GetSunPosition(sceneArea, (float)(Main.time / Main.dayLength));
            float distanceFromSun = sunPosition.Distance(noxusDrawPosition);
            float silhouetteInterpolant = GetLerpValue(85f, 21f, distanceFromSun, true);
            noxusDrawColor = Color.Lerp(noxusDrawColor, Color.Black, Pow(silhouetteInterpolant, 0.6f) * 0.85f);
            if (silhouetteInterpolant > 0f)
                noxusDrawColor.A = (byte)Lerp(noxusDrawColor.A, 255f, Pow(silhouetteInterpolant, 1.3f));

            EclipseDarknessInterpolant = GetLerpValue(58f, 21f, distanceFromSun, true);

            // Only draw the bloom effects, not the Noxus egg itself
            if (EclipseDarknessInterpolant > 0f)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

                Texture2D bloomFlare = ModContent.Request<Texture2D>("NoxusBossMod/Assets/ExtraTextures/BloomFlare").Value;
                Texture2D corona = ModContent.Request<Texture2D>("NoxusBossMod/Assets/ExtraTextures/GreyscaleTextures/Corona").Value;
                Main.spriteBatch.Draw(corona, sunPosition, null, Color.Wheat * EclipseDarknessInterpolant * 0.58f, Main.GlobalTimeWrappedHourly * 0.03f, corona.Size() * 0.5f, 0.26f, 0, 0f);
                Main.spriteBatch.Draw(bloomFlare, sunPosition, null, Color.LightGoldenrodYellow * EclipseDarknessInterpolant * 0.5f, Main.GlobalTimeWrappedHourly * 1.2f, bloomFlare.Size() * 0.5f, 0.3f, 0, 0f);
                Main.spriteBatch.Draw(bloomFlare, sunPosition, null, Color.LightGoldenrodYellow * EclipseDarknessInterpolant * 0.4f, Main.GlobalTimeWrappedHourly * -0.92f, bloomFlare.Size() * 0.5f, 0.25f, 0, 0f);
                
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }

            // Draw the egg without the blur effect (which was causing issues)
            float eggScale = Lerp(0.41f, 0.42f, Pow(EclipseDarknessInterpolant, 0.4f));
            Main.spriteBatch.Draw(noxusEggTexture, noxusDrawPosition, null, noxusDrawColor, 0f, noxusEggTexture.Size() * 0.5f, eggScale, 0, 0f);
        }

        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            backgroundColor = Color.Lerp(backgroundColor, new Color(8, 8, 11), EclipseDarknessInterpolant * 0.96f);
            tileColor = Color.Lerp(tileColor, Color.Black, EclipseDarknessInterpolant * 0.85f);
        }
    }
}