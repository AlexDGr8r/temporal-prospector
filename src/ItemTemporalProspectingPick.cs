using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TemporalProspector
{
    public class ItemTemporalProspectingPick : Item
    {

        private SimpleParticleProperties _particlesHeld;
        private SkillItem[] _toolModes;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            _particlesHeld = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(50, 220, 220, 220), 
                new Vec3d(), new Vec3d(), new Vec3f(), 
                new Vec3f(), 4f, 0.0f, 0.5f, 0.75f)
            {
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.6f),
                addLifeLength = 0.5f,
                WindAffected = false,
                WithTerrainCollision = false,
                VertexFlags = 220,
                SelfPropelled = true
            };
            
            _toolModes = ObjectCacheUtil.GetOrCreate(api, "temporalPickToolModes", () =>
            {
                SkillItem[] skillItemArray = {
                    new SkillItem
                    {
                        Code = new AssetLocation("shortradius"),
                        Name = Lang.Get("temporalprospector:temporal-prospecting-shortradius")
                    },
                    new SkillItem
                    {
                        Code = new AssetLocation("mediumradius"),
                        Name = Lang.Get("temporalprospector:temporal-prospecting-mediumradius")
                    },
                    new SkillItem
                    {
                        Code = new AssetLocation("longradius"),
                        Name = Lang.Get("temporalprospector:temporal-prospecting-longradius")
                    }
                };

                if (api is ICoreClientAPI capi)
                {
                    skillItemArray[0].WithIcon(capi, (cr, x, y, w, h, rgba) => 
                        DrawCircle(cr, x, y, w, h, rgba, 1D / 3D));
                    skillItemArray[1].WithIcon(capi, (cr, x, y, w, h, rgba) => 
                        DrawCircle(cr, x, y, w, h, rgba, 2D / 3D));
                    skillItemArray[2].WithIcon(capi, (cr, x, y, w, h, rgba) => 
                        DrawCircle(cr, x, y, w, h, rgba, 1));
                }

                return skillItemArray;
            });
        }

        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot,
            BlockSelection blockSel, float dropQuantityMultiplier = 1f)
        {
            var radius = 15; // short radius
            switch (GetToolMode(itemslot, null, blockSel))
            {
                case 1: // Medium radius
                    radius = 30;
                    break;
                case 2: // Long radius
                    radius = 60;
                    break;
            }
            
            ProspectArea(world, byEntity, blockSel, radius);

            if (DamagedBy != null && DamagedBy.Contains(EnumItemDamageSource.BlockBreaking))
            {
                DamageItem(world, byEntity, itemslot, radius / 3);
            }

            return true;
        }

        public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
        {
            foreach (var iSlot in allInputslots)
            {
                if (iSlot.Itemstack?.Item is ItemProspectingPick)
                {
                    var durability = iSlot.Itemstack.Attributes.GetInt("durability");
                    if (durability > 0)
                    {
                        outputSlot.Itemstack.Attributes.SetInt("durability", durability);
                    }
                    
                    break;
                }
            }
        }
        
        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            return _toolModes;
        }

        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => 
            Math.Min(_toolModes.Length - 1, slot.Itemstack.Attributes.GetInt("toolMode"));

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
        {
            slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
        }
        
        public override void OnUnloaded(ICoreAPI api)
        {
            base.OnUnloaded(api);
            for (var index = 0; _toolModes != null && index < _toolModes.Length; ++index)
            {
                _toolModes[index]?.Dispose();
            }
        }

        protected virtual void ProspectArea(IWorldAccessor world, Entity byEntity, BlockSelection blockSel, int radius)
        {
            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer player)
            {
                byPlayer = world.PlayerByUid(player.PlayerUID);
            }
                
            Block block = world.BlockAccessor.GetBlock(blockSel.Position);
            block.OnBlockBroken(world, blockSel.Position, byPlayer, 0.0f);
            if (!block.Code.Path.StartsWith("rock") && !block.Code.Path.StartsWith("ore") ||
                !(byPlayer is IServerPlayer serverPlayer))
            {
                return;
            }

            BlockPos blockPos = blockSel.Position.Copy();
            string resourceType = Variant["resource"];
            int numFound = 0;

            api.World.BlockAccessor.WalkBlocks(blockPos.AddCopy(radius, radius, radius),
                blockPos.AddCopy(-radius, -radius, -radius),
                (nblock, bp) =>
                {
                    if (nblock.BlockMaterial == EnumBlockMaterial.Ore && nblock.Variant.ContainsKey("type"))
                    {
                        if (nblock.Variant["type"].ToLower().Contains(resourceType))
                        {
                            numFound++;
                            SpawnParticles(world, blockPos.ToVec3d().Add(0.5D, 0.5D, 0.5D),
                                bp.ToVec3d().Add(0.5D, 0.5D, 0.5D));
                        }
                    }
                });

            serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup,
                "Found " + numFound + " " + resourceType + " nodes within " + radius + " blocks", EnumChatType.Notification);
        }
        
        private void SpawnParticles(IWorldAccessor world, Vec3d pos, Vec3d endPos)
        {
            int h = 110 + world.Rand.Next(15);
            int v = 100 + world.Rand.Next(50);
            _particlesHeld.MinPos = pos;
            _particlesHeld.MinVelocity = endPos.ToVec3f() - pos.ToVec3f();
            _particlesHeld.Color = ColorUtil.ReverseColorBytes(ColorUtil.HsvToRgba(h, 180, v));
            _particlesHeld.MinSize = 0.2f;
            _particlesHeld.ParticleModel = EnumParticleModel.Quad;
            _particlesHeld.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -150f);
            _particlesHeld.Color = ColorUtil.ReverseColorBytes(ColorUtil.HsvToRgba(h, 180, v, 150));
            world.SpawnParticles(_particlesHeld);
        }

        private void DrawCircle(Context cr, int x, int y, float width, float height, double[] rgba, double scale)
        {
            cr.SetSourceRGB(rgba[0], rgba[1], rgba[2]);
            cr.Translate(24, 24);
            cr.Scale(scale, scale);
            cr.Arc(0, 0, 18, 0, 2*Math.PI);
            cr.StrokePreserve();
            cr.Fill();
        }

    }
}