using System.Collections.Generic;
using System.Linq;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBossMod.Core.Graphics
{
    public class ScreenShatterSystem : ModSystem
    {
        public class ScreenTriangleShard
        {
            public float RotationX;
            public float RotationY;
            public float RotationZ;
            public Vector3 RotationalAxis;
            public Vector2 ScreenCoord1;
            public Vector2 ScreenCoord2;
            public Vector2 ScreenCoord3;
            public Vector2 DrawPosition;
            public Vector2 Velocity;

            public void Update()
            {
                float angularSlowdownInterpolant = GetLerpValue(0.112f, 1f, Velocity.Length(), true);
                RotationX += angularSlowdownInterpolant * RotationalAxis.X;
                RotationY += angularSlowdownInterpolant * RotationalAxis.Y;
                RotationZ += angularSlowdownInterpolant * RotationalAxis.Z;
                Velocity *= 0.91f;
                DrawPosition += GetLerpValue(0.97f, 0.55f, ShardOpacity, true) * Velocity;
            }

            public ScreenTriangleShard(Vector2 a, Vector2 b, Vector2 c, Vector2 drawPosition)
            {
                ScreenCoord1 = a;
                ScreenCoord2 = b;
                ScreenCoord3 = c;
                DrawPosition = drawPosition;
                Velocity = (DrawPosition - ShatterFocalPoint).RotatedByRandom(0.23f) * Main.rand.NextFloat(0.04f, 0.06f) + Main.rand.NextVector2CircularEdge(1.2f, 1.2f);
                RotationalAxis = new(Main.rand.NextFloatDirection() * 0.06f, Main.rand.NextFloatDirection() * 0.06f, Main.rand.NextFloatDirection() * 0.03f);
            }
        }

        public static float ShardOpacity { get; private set; }
        public static Vector2 ShatterFocalPoint { get; private set; }
        public static BasicEffect DrawShader { get; private set; }
        public static RenderTarget2D ContentsBeforeShattering { get; private set; }
        public static readonly List<ScreenTriangleShard> screenTriangles = [];
        public static readonly SoundStyle ShatterSound = new("NoxusBossMod/Assets/Sounds/Custom/ScreenShatter");
        private static bool isShattering = false;

        public override void Load()
        {
            Main.OnPostDraw += DrawShatterEffect;
            Main.QueueMainThreadAction(() =>
            {
                if (Main.netMode == NetmodeID.Server) return;
                DrawShader = new(Main.instance.GraphicsDevice) { TextureEnabled = true, VertexColorEnabled = true };
            });
        }

        public override void Unload()
        {
            Main.OnPostDraw -= DrawShatterEffect;
            Main.QueueMainThreadAction(() =>
            {
                DrawShader?.Dispose();
                ContentsBeforeShattering?.Dispose();
            });
        }

        private void DrawShatterEffect(GameTime obj)
        {
            if (screenTriangles.Count == 0 || ContentsBeforeShattering == null) return;

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Vector3 screenArea = new(Main.screenWidth, Main.screenHeight, 1f);
            List<VertexPositionColorTexture> shardVertices = [];
            Color shardColor = Color.White * Pow(ShardOpacity, 0.56f);

            foreach (ScreenTriangleShard shard in screenTriangles)
            {
                Vector3 a = new(shard.ScreenCoord1, 0f);
                Vector3 b = new(shard.ScreenCoord2, 0f);
                Vector3 c = new(shard.ScreenCoord3, 0f);
                Matrix shardTransformation = Matrix.CreateRotationX(shard.RotationX) * Matrix.CreateRotationY(shard.RotationY) * Matrix.CreateRotationZ(shard.RotationZ);
                a = Vector3.Transform(a, shardTransformation);
                b = Vector3.Transform(b, shardTransformation);
                c = Vector3.Transform(c, shardTransformation);
                a.Z = b.Z = c.Z = 0f;
                Vector3 center = (a + b + c) / 3f;
                shardVertices.Add(new((a - center) * screenArea + new Vector3(shard.DrawPosition, 0f), shardColor, shard.ScreenCoord1));
                shardVertices.Add(new((b - center) * screenArea + new Vector3(shard.DrawPosition, 0f), shardColor, shard.ScreenCoord2));
                shardVertices.Add(new((c - center) * screenArea + new Vector3(shard.DrawPosition, 0f), shardColor, shard.ScreenCoord3));
                shard.Update();
            }

            if (shardVertices.Count != 0)
            {
                CalamityUtils.CalculatePerspectiveMatricies(out Matrix effectView, out Matrix effectProjection);
                Vector2 scaleFactor = (MathHelper.SmoothStep(0f, 1f, 1f - ShardOpacity) * 3f + 1f) * Vector2.One / Main.GameViewMatrix.Zoom;
                DrawShader.View = effectView;
                DrawShader.Projection = effectProjection * Matrix.CreateScale(scaleFactor.X, scaleFactor.Y, 1f);
                DrawShader.Texture = ContentsBeforeShattering;
                DrawShader.CurrentTechnique.Passes[0].Apply();
                Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, shardVertices.ToArray(), 0, screenTriangles.Count);
            }

            ShardOpacity *= 0.95f;
            if (ShardOpacity <= 0.0045f) screenTriangles.Clear();
            Main.spriteBatch.End();
        }

        public static void TriggerShatter(Vector2 worldPosition)
        {
            if (!NoxusBossConfig.Instance.ScreenShatterEffects)
            {
                Main.LocalPlayer.Calamity().GeneralScreenShakePower += 11f;
                return;
            }

            if (isShattering) return;
            isShattering = true;

            ShatterFocalPoint = worldPosition - Main.screenPosition;
            ShardOpacity = 1f;

            
            try
            {
                var gd = Main.instance.GraphicsDevice;

                if (ContentsBeforeShattering == null || ContentsBeforeShattering.IsDisposed || ContentsBeforeShattering.Width != Main.screenWidth)
                {
                    ContentsBeforeShattering?.Dispose();
                    ContentsBeforeShattering = new(gd, Main.screenWidth, Main.screenHeight, true, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
                }

                gd.SetRenderTarget(ContentsBeforeShattering);
                gd.Clear(Color.Transparent);
                Main.spriteBatch.Begin();
                Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
                Main.spriteBatch.End();
                gd.SetRenderTarget(null);

                screenTriangles.Clear();

                int radialSliceCount = Main.rand.Next(8, 12);
                List<float> radialSliceAngles = [];
                Rectangle screenRect = new(0, 0, Main.screenWidth, Main.screenHeight);

                for (int i = 0; i < radialSliceCount; i++)
                {
                    float sliceAngle = TwoPi * i / radialSliceCount + Main.rand.NextFloatDirection() * 0.00146f;
                    radialSliceAngles.Add(sliceAngle);
                }

                for (int i = 0; i < radialSliceCount; i++)
                {
                    Vector2 a = ShatterFocalPoint / screenRect.Size();
                    Vector2 b = a + radialSliceAngles[i].ToRotationVector2() * 0.7f;
                    Vector2 c = a + radialSliceAngles[(i + 1) % radialSliceCount].ToRotationVector2() * 0.7f;
                    screenTriangles.Add(new(a, b, c, (a + b + c) / 3f * screenRect.Size()));
                }

                while (screenTriangles.Count <= 180)
                {
                    var shard = Main.rand.Next(screenTriangles);
                    SubdivideRadialTriangle(shard, Main.rand.NextFloat(0.15f, 0.44f), Main.rand.NextFloat(0.15f, 0.44f), screenTriangles);
                }

                ScreenEffectSystem.SetFlashEffect(ShatterFocalPoint + Main.screenPosition, 2f, 32);
                SoundEngine.PlaySound(ShatterSound);
                isShattering = false;
            }
            catch
            {
                isShattering = false;
            }
        }

        private static void SubdivideRadialTriangle(ScreenTriangleShard shard, float line1BreakInterpolant, float line2BreakInterpolant, List<ScreenTriangleShard> shards)
        {
            shards.Remove(shard);
            Vector2 center = shard.ScreenCoord1, left = shard.ScreenCoord2, right = shard.ScreenCoord3;
            Vector2 lineBreakLeft = Vector2.Lerp(center, left, line1BreakInterpolant);
            Vector2 lineBreakRight = Vector2.Lerp(center, right, line2BreakInterpolant);
            shards.Add(new(center, lineBreakLeft, lineBreakRight, new Vector2(Main.screenWidth, Main.screenHeight) * (center + lineBreakLeft + lineBreakRight) / 3f));
            shards.Add(new(right, left, lineBreakLeft, new Vector2(Main.screenWidth, Main.screenHeight) * (left + right + lineBreakLeft) / 3f));
            shards.Add(new(right, lineBreakLeft, lineBreakRight, new Vector2(Main.screenWidth, Main.screenHeight) * (right + lineBreakLeft + lineBreakRight) / 3f));
        }
    }
}