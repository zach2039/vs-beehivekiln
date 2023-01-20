using beehivekiln.blockentity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace beehivekiln.block
{
	public class BlockFirebrickKilnFlue : Block
	{
		public bool ControllerBlock
		{
			get
			{
				return this.EntityClass != null;
			}
		}


		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			BlockPos pos = blockSel.Position;
			if (pos == null)
			{
				return base.OnBlockInteractStart(world, byPlayer, blockSel);
			}
			BlockEntityBeehiveKiln bebk = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityBeehiveKiln;
			if (bebk != null)
			{
				bebk.Interact(byPlayer, !this.ControllerBlock);
				IClientPlayer clientPlayer = byPlayer as IClientPlayer;
				if (clientPlayer != null)
				{
					clientPlayer.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				}
				return true;
			}
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}

		public int GetTemperature(IWorldAccessor world, BlockPos pos)
		{
			if (pos == null)
			{
				return 0;
			}
			BlockEntityBeehiveKiln besc = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityBeehiveKiln;
			if (besc != null)
			{
				return besc.KilnTemperature;
			}
			return 0;
		}

		public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
		{
			base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
			world.BlockAccessor.RemoveBlockEntity(pos);
		}
	}
}
