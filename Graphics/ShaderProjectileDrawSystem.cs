using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBossMod.Core.Graphics
{
    public class ShaderProjectileDrawSystem : ModSystem
    {
        public override void PostDrawTiles()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.ModProjectile is IDrawsWithShader drawer)
                {
                    drawer.Draw(Main.spriteBatch);
                }
            }

            Main.spriteBatch.End();
        }
    }
}