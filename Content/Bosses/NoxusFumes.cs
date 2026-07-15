using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace NoxusBossMod.Content.Bosses
{
    public class NoxusFumes : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            Main.pvpBuff[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense -= 15;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.defense -= 15;
        }

        public static void CreateIllusions(Player player)
        {
            
            for (int i = 0; i < 3; i++)
            {
                Dust dust = Dust.NewDustDirect(player.Center + new Vector2(Main.rand.NextFloat(-50f, 50f), Main.rand.NextFloat(-50f, 50f)), 1, 1, DustID.PurpleCrystalShard);
                dust.velocity = player.velocity * 0.5f;
                dust.noGravity = true;
                dust.scale = 1.2f;
            }
        }
    }
}