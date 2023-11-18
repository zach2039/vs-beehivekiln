using beehivekiln.block;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace beehivekiln.blockentity
{
	/// <summary>
	/// The base functionality and error checking mostly comes from base game's StoneCoffinSection and similar
	/// </summary>
    public class BlockEntityBeehiveKiln : BlockEntity
	{

		public int KilnTemperature
		{
			get
			{
				return this.tempKiln;
			}
		}

		public int MinKilnTemperature
        {
			get
            {
				//return 500;
				return BeehiveKilnConfig.Loaded.MinimumFiringTemperatureCelsius;
			}
        }

		protected virtual int EnvironmentTemperature()
		{
			ClimateCondition conds = this.Api.World.BlockAccessor.GetClimateAt(this.Pos, EnumGetClimateMode.NowValues, 0.0);
			if (conds != null)
			{
				return (int)conds.Temperature;
			}
			return 20;
		}

		public bool Interact(IPlayer byPlayer, bool preferThis)
		{
			bool sneaking = byPlayer.WorldData.EntityControls.ShiftKey;
			int[] wrongTiles = new int[this.msPossibleList.Count];
			int[] incompleteCount = new int[this.msPossibleList.Count];
			int bestIdx = 0;
			BlockPos posMain = this.Pos;
			if (sneaking)
			{
				for (int i = 0; i < this.msPossibleList.Count; i++)
                {
					int wt = 0;
					int ic = this.msPossibleList[i].InCompleteBlockCount(this.Api.World, this.Pos, delegate (Block haveBlock, AssetLocation wantLoc)
					{
						int num;
						num = wt;
						wt = num + 1;
					});
					incompleteCount[i] = ic;
					wrongTiles[i] = wt;
				}

				int icLast = int.MaxValue;
				for (int i = 0; i < this.msPossibleList.Count; i++)
                {
					if (incompleteCount[i] < icLast)
                    {
						icLast = incompleteCount[i];
						bestIdx = i;
                    }
                }

				if (incompleteCount[bestIdx] > 0)
				{
					this.msHighlighted = this.msPossibleList[bestIdx];
				}
			}

			if (sneaking && incompleteCount[bestIdx] > 0)
			{
				if (wrongTiles[bestIdx] > 0)
				{
					ICoreClientAPI coreClientAPI2 = this.capi;
					if (coreClientAPI2 != null)
					{
						coreClientAPI2.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} blocks are missing or wrong!", new object[]
						{
							wrongTiles[bestIdx]
						}));
					}
				}
				if (this.Api.Side == EnumAppSide.Client)
				{
					this.msHighlighted.HighlightIncompleteParts(this.Api.World, byPlayer, posMain);
				}
				return false;
			}
			if (this.Api.Side == EnumAppSide.Client)
			{
				MultiblockStructure multiblockStructure = this.msHighlighted;
				if (multiblockStructure != null)
				{
					multiblockStructure.ClearHighlights(this.Api.World, byPlayer);
				}
			}
			if (!sneaking)
			{
				return false;
			}
			ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
			if (slot.Empty)
			{
				return true;
			}
			return false;
		}

		private void onServerTick1s(float dt)
		{
			int i;
			if (this.hotEnough)
			{
				Vec3d pos = this.Pos.ToVec3d().Add(0.5, -2.5, 0.5);
				Entity[] entitiesAround = this.Api.World.GetEntitiesAround(pos, 2.5f, 0.5f, (Entity e) => e.Alive && e is EntityAgent);
				for (i = 0; i < entitiesAround.Length; i++)
				{
					entitiesAround[i].ReceiveDamage(new DamageSource
					{
						DamageTier = 1,
						SourcePos = pos,
						SourceBlock = base.Block,
						Type = EnumDamageType.Fire
					}, 4f);
				}
			}
			i = this.tickCounter + 1;
			this.tickCounter = i;
			if (i % 3 == 0)
			{
				this.onServerTick3s(dt);
			}
		}

		/// <summary>
		/// Passes fueled values and burn temperature of a fire pit heat source.
		/// </summary>
		/// <param name="pos">position of possible heat source</param>
		/// <returns></returns>
		private void GetHeatSourceDetails(BlockPos pos, ref bool isFueled, ref int temperature)
        {
			BlockEntity fuelPileBlock = this.Api.World.BlockAccessor.GetBlockEntity(pos);
			if (fuelPileBlock != null)
			{
				BlockEntityFirepit firepitBE = fuelPileBlock as BlockEntityFirepit;
				if (firepitBE != null)
                {
					float outsideTemp = this.EnvironmentTemperature();
					if (firepitBE.IsBurning)
                    {
						temperature = (int)Math.Round(firepitBE.furnaceTemperature);
					}
					else if (firepitBE.furnaceTemperature > outsideTemp)
                    {
						temperature = (int)Math.Round(firepitBE.furnaceTemperature);
					}
					else
                    {
						temperature = (int)Math.Round(outsideTemp);
					}
					
					if (firepitBE.fuelCombustibleOpts != null || firepitBE.IsBurning)
                    {
						isFueled = true;
					}
				}
			}
		}

		private void onServerTick3s(float dt)
		{
			BlockPos fuelPilePos = this.Pos.DownCopy(4);
			bool beforeHotEnough = this.hotEnough;
			bool beforeStructureComplete = this.structureComplete;
			int temp = this.EnvironmentTemperature();
			this.fueled = false;
			
			if (beforeStructureComplete != this.structureComplete)
            {
				this.totalHoursLastUpdate = this.Api.World.Calendar.TotalHours;
			}

			double hoursPassed = this.Api.World.Calendar.TotalHours - this.totalHoursLastUpdate;
			this.totalHoursLastUpdate = this.Api.World.Calendar.TotalHours;

			GetHeatSourceDetails(fuelPilePos, ref this.fueled, ref temp);

			MultiblockStructure msInUse = null;
			this.structureComplete = false;
			// Scan for suitable multiblock
			foreach (MultiblockStructure msPossible in this.msPossibleList)
            {
				this.incompleteBlockCount = msPossible.InCompleteBlockCount(this.Api.World, this.Pos, null);
				if (this.incompleteBlockCount == 0)
                {
					this.structureComplete = true;
					msInUse = msPossible;
					break;
                }
			}
			
			if (!this.structureComplete)
			{
				this.progress = 0f;
				this.processComplete = false;
				this.hotEnough = false;
				this.tempKiln = this.EnvironmentTemperature();
				base.MarkDirty(true, null);
				return;
			}

			if (Math.Abs((float)this.tempKiln - temp) > 25f)
			{
				//float tempGain = (float)(250 * hoursPassed);
				float tempGain = (float)(BeehiveKilnConfig.Loaded.TemperatureGainPerHourCelsius * hoursPassed);
				if (this.tempKiln > temp)
				{
					this.tempKiln = (int)Math.Max(this.tempKiln - tempGain, temp);
				}
				else
				{
					this.tempKiln = (int)Math.Min(this.tempKiln + tempGain, temp);
				}

				if (this.tempKiln > this.MinKilnTemperature)
				{
					this.hotEnough = true;
				}
				else
                {
					this.hotEnough = false;
				}

				base.MarkDirty(true, null);
			}

			if (this.processComplete)
			{
				return;
			}

			if (beforeHotEnough != this.hotEnough || beforeStructureComplete != this.structureComplete)
			{
				base.MarkDirty(false, null);
			}

			if (this.hotEnough)
			{
				//this.progress += hoursPassed / 6.0;
				this.progress += hoursPassed / BeehiveKilnConfig.Loaded.FiringTimeHours;
				base.MarkDirty(true, null);
			}

			if (this.progress >= 0.995 && !this.processComplete)
			{
				// For each block contained in the kiln chamber, fire it when kiln is done
				for (int x = -1; x < 2; x++)
                {
					for (int z = -1; z < 2; z++)
                    {
						BlockPos unfiredBlockPos = this.Pos.AddCopy(x, -2, z);
						BlockEntityGroundStorage beg = this.Api.World.BlockAccessor.GetBlockEntity(unfiredBlockPos) as BlockEntityGroundStorage;
						if (beg != null)
                        {
							// More or less taken from pit kiln code
							for (int i = 0; i < 4; i++)
                            {
								ItemSlot slot = beg.Inventory[i];
								if (!slot.Empty)
								{
									ItemStack rawStack = slot.Itemstack;
									CombustibleProperties combustibleProps = rawStack.Collectible.CombustibleProps;
									ItemStack itemStack;
									if (combustibleProps == null)
									{
										itemStack = null;
									}
									else
									{
										JsonItemStack smeltedStack = combustibleProps.SmeltedStack;
										itemStack = (smeltedStack != null) ? smeltedStack.ResolvedItemstack : null;
									}
									ItemStack firedStack = itemStack;
									if (firedStack != null)
									{
										slot.Itemstack = firedStack.Clone();
										slot.Itemstack.StackSize = rawStack.StackSize / rawStack.Collectible.CombustibleProps.SmeltedRatio;
									}
								}
							}
							beg.MarkDirty(true, null);
                        }
					}
                }
				this.processComplete = true;
			}
		}

		private void onClientTick50ms(float dt)
		{
			if (this.processComplete || !this.structureComplete)
			{
				return;
			}
			if (!this.fueled || !this.hotEnough)
			{
				return;
			}
			Random rnd = this.Api.World.Rand;
			for (int i = 0; i < Entity.FireParticleProps.Length; i++)
			{
				int index = Math.Min(Entity.FireParticleProps.Length - 1, this.Api.World.Rand.Next(Entity.FireParticleProps.Length + 1));
				AdvancedParticleProperties particles = Entity.FireParticleProps[index];
				for (int j = 0; j < this.particlePositions.Length; j++)
				{
					BlockPos pos = this.particlePositions[j];
					if (j >= 0)
					{
						particles = BlockEntityBeehiveKiln.smokeParticles;
						particles.Quantity.avg = 0.2f;
						particles.basePos.Set((double)pos.X + 0.5, (double)pos.Y + 0.75, (double)pos.Z + 0.5);
						particles.Velocity[1].avg = (float)(0.3 + 0.3 * rnd.NextDouble()) * 2f;
						particles.PosOffset[1].var = 0.2f;
						particles.Velocity[0].avg = (float)(rnd.NextDouble() - 0.5) / 4f;
						particles.Velocity[2].avg = (float)(rnd.NextDouble() - 0.5) / 4f;
					}
					else
					{
						particles.Quantity.avg = GameMath.Sqrt(0.5f * ((index == 0) ? 0.5f : ((index == 1) ? 5f : 0.6f))) / 2f;
						particles.basePos.Set((double)pos.X + 0.5, (double)pos.Y + 0.5, (double)pos.Z + 0.5);
						particles.Velocity[1].avg = (float)(0.5 + 0.5 * rnd.NextDouble()) * 2f;
						particles.PosOffset[1].var = 1f;
						particles.Velocity[0].avg = (float)(rnd.NextDouble() - 0.5);
						particles.Velocity[2].avg = (float)(rnd.NextDouble() - 0.5);
					}
					particles.PosOffset[0].var = 0.49f;
					particles.PosOffset[2].var = 0.49f;
					this.Api.World.SpawnParticles(particles, null);
				}
			}
		}
		public override void Initialize(ICoreAPI api)
		{
			base.Initialize(api);
			BlockPos pos = this.Pos;
			this.capi = (api as ICoreClientAPI);
			if (api.Side == EnumAppSide.Client)
			{
				this.RegisterGameTickListener(new Action<float>(this.onClientTick50ms), 50, 0);
			}
			else
			{
				this.RegisterGameTickListener(new Action<float>(this.onServerTick1s), 1000, 0);
			}

			// Get each multiblock defined in attributes, and rotate them around to get 4 versions of it for later checking
			foreach (JsonObject msPossible in base.Block.Attributes["multiblockStructures"].AsArray())
			{
				MultiblockStructure msNorth = msPossible.AsObject<MultiblockStructure>(null);
				MultiblockStructure msEast = msPossible.AsObject<MultiblockStructure>(null);
				MultiblockStructure msSouth = msPossible.AsObject<MultiblockStructure>(null);
				MultiblockStructure msWest = msPossible.AsObject<MultiblockStructure>(null);

				msNorth.InitForUse(0);
				msEast.InitForUse(90);
				msSouth.InitForUse(180);
				msWest.InitForUse(270);

				this.msPossibleList.Add(msNorth);
				this.msPossibleList.Add(msEast);
				this.msPossibleList.Add(msSouth);
				this.msPossibleList.Add(msWest);
			}

			//this.msPossibleList[BlockFacing.NORTH.Index] = base.Block.Attributes["multiblockStructure"].AsObject<MultiblockStructure>(null);
			//this.msPossibleList[BlockFacing.EAST.Index] = base.Block.Attributes["multiblockStructure"].AsObject<MultiblockStructure>(null);
			//this.msPossibleList[BlockFacing.SOUTH.Index] = base.Block.Attributes["multiblockStructure"].AsObject<MultiblockStructure>(null);
			//this.msPossibleList[BlockFacing.WEST.Index] = base.Block.Attributes["multiblockStructure"].AsObject<MultiblockStructure>(null);

			//this.msPossibleList[BlockFacing.NORTH.Index].InitForUse(0);
			//this.msPossibleList[BlockFacing.EAST.Index].InitForUse(90);
			//this.msPossibleList[BlockFacing.SOUTH.Index].InitForUse(180);
			//this.msPossibleList[BlockFacing.WEST.Index].InitForUse(270);

			this.blockFbg = (base.Block as BlockFirebrickKilnFlue);
			this.particlePositions[0] = this.Pos;
			this.totalHoursLastUpdate = this.Api.World.Calendar.TotalHours;
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.ToTreeAttributes(tree);
			tree.SetBool("hotEnough", this.hotEnough);
			tree.SetDouble("totalHoursLastUpdate", this.totalHoursLastUpdate);
			tree.SetDouble("progress", this.progress);
			tree.SetBool("processComplete", this.processComplete);
			tree.SetBool("structureComplete", this.structureComplete);
			tree.SetInt("incompleteBlockCount", this.incompleteBlockCount);
			tree.SetBool("fueled", this.fueled);
			tree.SetInt("tempKiln", this.tempKiln);
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
		{
			base.FromTreeAttributes(tree, worldAccessForResolve);
			this.hotEnough = tree.GetBool("hotEnough", false);
			this.totalHoursLastUpdate = tree.GetDouble("totalHoursLastUpdate", 0.0);
			this.progress = tree.GetDouble("progress", 0.0);
			this.processComplete = tree.GetBool("processComplete", false);
			this.structureComplete = tree.GetBool("structureComplete", false);
			this.incompleteBlockCount = tree.GetInt("incompleteBlockCount", 0);
			this.fueled = tree.GetBool("fueled", false);
			this.tempKiln = tree.GetInt("tempKiln", 0);
		}

		public override void OnBlockRemoved()
		{
			base.OnBlockRemoved();
			if (this.Api.Side == EnumAppSide.Client)
			{
				MultiblockStructure multiblockStructure = this.msHighlighted;
				if (multiblockStructure == null)
				{
					return;
				}
				multiblockStructure.ClearHighlights(this.Api.World, (this.Api as ICoreClientAPI).World.Player);
			}
		}

		public override void OnBlockUnloaded()
		{
			base.OnBlockUnloaded();
			ICoreAPI api = this.Api;
			if (api != null && api.Side == EnumAppSide.Client)
			{
				MultiblockStructure multiblockStructure = this.msHighlighted;
				if (multiblockStructure == null)
				{
					return;
				}
				multiblockStructure.ClearHighlights(this.Api.World, (this.Api as ICoreClientAPI).World.Player);
			}
		}

		private void appendInfo(StringBuilder dsc)
        {
			dsc.AppendLine(Lang.Get("Temperature: {0}°C", new object[] { (int)(this.tempKiln) }));

			/// debug
			// dsc.AppendLine(Lang.Get("Fueled: {0}", new object[] { (bool)(this.fueled) }));
			// dsc.AppendLine(Lang.Get("Incomplete blocks: {0}", new object[] { (int)(this.incompleteBlockCount) }));
			// dsc.AppendLine(Lang.Get("Structure Complete: {0}", new object[] { (bool)(this.structureComplete) }));
			// dsc.AppendLine(Lang.Get("Process Complete: {0}", new object[] { (bool)(this.processComplete) }));
			// dsc.AppendLine(Lang.Get("Progress: {0}", new object[] { (float)(this.progress) }));
			// dsc.AppendLine(Lang.Get("Total Hours Last Update: {0}", new object[] { (float)(this.totalHoursLastUpdate) }));
			// dsc.AppendLine(Lang.Get("Total Hours: {0}", new object[] { (float)(this.Api.World.Calendar.TotalHours) }));
			// dsc.AppendLine(Lang.Get("Hot Enough: {0}", new object[] { (bool)(this.hotEnough) }));
			/// debug end
		}

		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
			if (this.processComplete)
			{
				dsc.AppendLine(Lang.Get("Firing process complete.", Array.Empty<object>()));
				appendInfo(dsc);
				return;
			}
			if (!this.structureComplete)
			{
				dsc.AppendLine(Lang.Get("Structure incomplete! Firing progress is reset.", Array.Empty<object>()));
				appendInfo(dsc);
				return;
			}
			if (this.hotEnough)
			{
				dsc.AppendLine(Lang.Get("Adequate kiln temperature. Firing...", Array.Empty<object>()));
				appendInfo(dsc);
			}
			else
			{
				dsc.AppendLine(Lang.Get("Kiln temperature is too cold! Must be {0}°C or hotter.", new object[] { this.MinKilnTemperature }));
				dsc.AppendLine(Lang.Get("Ignite a firepit under the center grate of the kiln.", Array.Empty<object>()));
				appendInfo(dsc);
			}
			if (this.progress > 0.0)
			{
				dsc.AppendLine(Lang.Get("Firing: {0}% complete", new object[]
				{
					(int)(this.progress * 100.0)
				}));
			}
		}

		private static AdvancedParticleProperties smokeParticles = new AdvancedParticleProperties
		{
			HsvaColor = new NatFloat[]
			{
				NatFloat.createUniform(0f, 0f),
				NatFloat.createUniform(0f, 0f),
				NatFloat.createUniform(40f, 30f),
				NatFloat.createUniform(220f, 50f)
			},
			OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
			GravityEffect = NatFloat.createUniform(0f, 0f),
			Velocity = new NatFloat[]
			{
				NatFloat.createUniform(0f, 0.05f),
				NatFloat.createUniform(0.2f, 0.3f),
				NatFloat.createUniform(0f, 0.05f)
			},
			Size = NatFloat.createUniform(0.3f, 0.05f),
			Quantity = NatFloat.createUniform(0.25f, 0f),
			SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
			LifeLength = NatFloat.createUniform(4.5f, 0f),
			ParticleModel = EnumParticleModel.Quad,
			SelfPropelled = true
		};

		private List<MultiblockStructure> msPossibleList = new List<MultiblockStructure>();

		private MultiblockStructure msHighlighted;

		private BlockFirebrickKilnFlue blockFbg;

		private ICoreClientAPI capi;

		private bool hotEnough;

		private double progress;

		private double totalHoursLastUpdate;

		private bool processComplete;

		private bool structureComplete;

		private int tickCounter;

		private int incompleteBlockCount;

		private bool fueled;

		private int tempKiln;

		private BlockPos[] particlePositions = new BlockPos[1];
	}
}
