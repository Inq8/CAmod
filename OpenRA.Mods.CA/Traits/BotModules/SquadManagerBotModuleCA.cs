#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Manages AI squads.")]
	public class SquadManagerBotModuleCAInfo : ConditionalTraitInfo
	{
		[Desc("Actor types that are valid for naval squads.")]
		public readonly HashSet<string> NavalUnitsTypes = new HashSet<string>();

		[Desc("Actor types that should generally be excluded from attack squads.")]
		public readonly HashSet<string> ExcludeFromSquadsTypes = new HashSet<string>();

		[Desc("Actor types that should generally be excluded from air squads.")]
		public readonly HashSet<string> ExcludeFromAirSquadsTypes = new HashSet<string>();

		[Desc("Actor types that are considered construction yards (base builders).")]
		public readonly HashSet<string> ConstructionYardTypes = new HashSet<string>();

		[Desc("Enemy building types around which to scan for targets for naval squads.")]
		public readonly HashSet<string> NavalProductionTypes = new HashSet<string>();

		[Desc("Units that form a guerrilla squad.")]
		public readonly HashSet<string> GuerrillaTypes = new HashSet<string>();

		[Desc("Minimum number of units AI must have before attacking.")]
		public readonly int SquadSize = 8;

		[Desc("Random number of up to this many units is added to squad size when creating an attack squad.")]
		public readonly int SquadSizeRandomBonus = 30;

		[Desc("Possibility of units in GuerrillaTypes to join Guerrilla.")]
		public readonly int JoinGuerrilla = 50;

		[Desc("Max number of units AI has in guerrilla squad")]
		public readonly int MaxGuerrillaSize = 10;

		[Desc("Delay (in ticks) between giving out orders to units.")]
		public readonly int AssignRolesInterval = 50;

		[Desc("Delay (in ticks) between updating squads.")]
		public readonly int AttackForceInterval = 75;

		[Desc("Minimum delay (in ticks) between creating squads.")]
		public readonly int MinimumAttackForceDelay = 0;

		[Desc("Radius in cells around the base that should be scanned for units to be protected.")]
		public readonly int ProtectUnitScanRadius = 15;

		[Desc("Maximum distance in cells from center of the base when checking for MCV deployment location.",
			"Only applies if RestrictMCVDeploymentFallbackToBase is enabled and there's at least one construction yard.")]
		public readonly int MaxBaseRadius = 20;

		[Desc("Radius in cells that squads should scan for enemies around their position while idle.")]
		public readonly int IdleScanRadius = 10;

		[Desc("Radius in cells that squads should scan for danger around their position to make flee decisions.")]
		public readonly int DangerScanRadius = 10;

		[Desc("Radius in cells that attack squads should scan for enemies around their position when trying to attack.")]
		public readonly int AttackScanRadius = 12;

		[Desc("Radius in cells that protecting squads should scan for enemies around their position.")]
		public readonly int ProtectionScanRadius = 8;

		[Desc("Enemy target types to never target.")]
		public readonly BitSet<TargetableType> IgnoredEnemyTargetTypes = default(BitSet<TargetableType>);

		[Desc("Locomotor used by pathfinding leader for squads")]
		public readonly HashSet<string> SuggestedLeaderLocomotor = new HashSet<string>();

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (DangerScanRadius <= 0)
				throw new YamlException("DangerScanRadius must be greater than zero.");
		}

		public override object Create(ActorInitializer init) { return new SquadManagerBotModuleCA(init.Self, this); }
	}

	public class SquadManagerBotModuleCA : ConditionalTrait<SquadManagerBotModuleCAInfo>, IBotEnabled, IBotTick, IBotRespondToAttack, IBotPositionsUpdated, IGameSaveTraitData
	{
		public CPos GetRandomBaseCenter()
		{
			var randomConstructionYard = World.Actors.Where(a => a.Owner == Player &&
				Info.ConstructionYardTypes.Contains(a.Info.Name))
				.RandomOrDefault(World.LocalRandom);

			return randomConstructionYard?.Location ?? initialBaseCenter;
		}

		public readonly World World;
		public readonly Player Player;

		public readonly Predicate<Actor> UnitCannotBeOrdered;
		readonly List<Actor> unitsHangingAroundTheBase = new List<Actor>();

		// Units that the bot already knows about. Any unit not on this list needs to be given a role.
		readonly List<Actor> activeUnits = new List<Actor>();

		public List<SquadCA> Squads = new List<SquadCA>();

		IBot bot;
		IBotPositionsUpdated[] notifyPositionsUpdated;
		IBotNotifyIdleBaseUnits[] notifyIdleBaseUnits;

		CPos initialBaseCenter;

		int assignRolesTicks;

		// int attackForceTicks;
		int protectionForceTicks;
		int guerrillaForceTicks;
		int airForceTicks;
		int navyForceTicks;
		int groundForceTicks;

		int minAttackForceDelayTicks;

		public SquadManagerBotModuleCA(Actor self, SquadManagerBotModuleCAInfo info)
			: base(info)
		{
			World = self.World;
			Player = self.Owner;

			UnitCannotBeOrdered = a => a == null || a.Owner != Player || a.IsDead || !a.IsInWorld;
		}

		// Use for proactive targeting.
		public bool IsPreferredEnemyUnit(Actor a)
		{
			if (a == null || a.IsDead || Player.RelationshipWith(a.Owner) != PlayerRelationship.Enemy || a.Info.HasTraitInfo<HuskInfo>())
				return false;

			var targetTypes = a.GetEnabledTargetTypes();
			return !targetTypes.IsEmpty && !targetTypes.Overlaps(Info.IgnoredEnemyTargetTypes);
		}

		public bool IsNotHiddenUnit(Actor a)
		{
			var hasModifier = false;
			var visModifiers = a.TraitsImplementing<IVisibilityModifier>();
			foreach (var v in visModifiers)
			{
				if (v.IsVisible(a, Player))
					return true;

				hasModifier = true;
			}

			return !hasModifier;
		}

		protected override void Created(Actor self)
		{
			notifyPositionsUpdated = self.Owner.PlayerActor.TraitsImplementing<IBotPositionsUpdated>().ToArray();
			notifyIdleBaseUnits = self.Owner.PlayerActor.TraitsImplementing<IBotNotifyIdleBaseUnits>().ToArray();
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			assignRolesTicks = World.LocalRandom.Next(0, Info.AssignRolesInterval);

			var attackForceTicks = World.LocalRandom.Next(0, Info.AttackForceInterval);

			protectionForceTicks = attackForceTicks;
			guerrillaForceTicks = attackForceTicks + 1;
			airForceTicks = attackForceTicks + 2;
			navyForceTicks = attackForceTicks + 3;
			groundForceTicks = attackForceTicks + 4;

			minAttackForceDelayTicks = World.LocalRandom.Next(0, Info.MinimumAttackForceDelay);
		}

		void IBotEnabled.BotEnabled(IBot bot)
		{
			this.bot = bot;
		}

		void IBotTick.BotTick(IBot bot)
		{
			AssignRolesToIdleUnits(bot);
		}

		internal Actor FindClosestEnemy(WPos pos)
		{
			var units = World.Actors.Where(IsPreferredEnemyUnit);
			return units.Where(IsNotHiddenUnit).ClosestTo(pos) ?? units.ClosestTo(pos);
		}

		internal Actor FindClosestEnemy(WPos pos, WDist radius)
		{
			return World.FindActorsInCircle(pos, radius).Where(a => IsPreferredEnemyUnit(a) && IsNotHiddenUnit(a)).ClosestTo(pos);
		}

		void CleanSquads()
		{
			Squads.RemoveAll(s => !s.IsValid);
			foreach (var s in Squads)
				s.Units.RemoveAll(UnitCannotBeOrdered);
		}

		// HACK: Use of this function requires that there is one squad of this type.
		SquadCA GetSquadOfType(SquadCAType type)
		{
			return Squads.FirstOrDefault(s => s.Type == type);
		}

		IEnumerable<SquadCA> GetSquadsOfType(SquadCAType type)
		{
			return Squads.Where(s => s.Type == type);
		}

		SquadCA RegisterNewSquad(IBot bot, SquadCAType type, Actor target = null)
		{
			var ret = new SquadCA(bot, this, type, target);
			Squads.Add(ret);
			return ret;
		}

		public void DismissSquad(SquadCA squad)
		{
			foreach (var unit in squad.Units)
			{
				unitsHangingAroundTheBase.Add(unit);
			}

			squad.Units.Clear();
		}

		void AssignRolesToIdleUnits(IBot bot)
		{
			CleanSquads();

			activeUnits.RemoveAll(UnitCannotBeOrdered);
			unitsHangingAroundTheBase.RemoveAll(UnitCannotBeOrdered);
			foreach (var n in notifyIdleBaseUnits)
				n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);

			// Ticks squads
			if (--protectionForceTicks <= 0)
			{
				protectionForceTicks = Info.AttackForceInterval;
				foreach (var s in GetSquadsOfType(SquadCAType.Protection))
					s.Update();
			}

			if (--guerrillaForceTicks <= 0)
			{
				guerrillaForceTicks = Info.AttackForceInterval;
				foreach (var s in GetSquadsOfType(SquadCAType.Assault))
					s.Update();
			}

			if (--airForceTicks <= 0)
			{
				airForceTicks = Info.AttackForceInterval;
				foreach (var s in GetSquadsOfType(SquadCAType.Air))
					s.Update();
			}

			if (--navyForceTicks <= 0)
			{
				navyForceTicks = Info.AttackForceInterval;
				foreach (var s in GetSquadsOfType(SquadCAType.Naval))
					s.Update();
			}

			if (--groundForceTicks <= 0)
			{
				groundForceTicks = Info.AttackForceInterval;
				foreach (var s in GetSquadsOfType(SquadCAType.Rush))
					s.Update();
			}

			if (--assignRolesTicks <= 0)
			{
				assignRolesTicks = Info.AssignRolesInterval;
				FindNewUnits(bot);
			}

			if (--minAttackForceDelayTicks <= 0)
			{
				minAttackForceDelayTicks = Info.MinimumAttackForceDelay;
				CreateAttackForce(bot);
			}
		}

		void FindNewUnits(IBot bot)
		{
			var newUnits = World.ActorsHavingTrait<IPositionable>()
				.Where(a => a.Owner == Player &&
					!Info.ExcludeFromSquadsTypes.Contains(a.Info.Name) &&
					!activeUnits.Contains(a));

			var guerrillaForce = GetSquadOfType(SquadCAType.Assault);
			var guerrillaUpdate = guerrillaForce == null ? true : guerrillaForce.Units.Count <= Info.MaxGuerrillaSize && (World.LocalRandom.Next(100) >= Info.JoinGuerrilla);

			foreach (var a in newUnits)
			{
				if (Info.GuerrillaTypes.Contains(a.Info.Name) && guerrillaUpdate)
				{
					if (guerrillaForce == null)
						guerrillaForce = RegisterNewSquad(bot, SquadCAType.Assault);

					guerrillaForce.Units.Add(a);
				}
				else if (a.Info.HasTraitInfo<AircraftInfo>() && a.Info.HasTraitInfo<AttackBaseInfo>() && !Info.ExcludeFromAirSquadsTypes.Contains(a.Info.Name))
				{
					var air = GetSquadOfType(SquadCAType.Air);
					if (air == null)
						air = RegisterNewSquad(bot, SquadCAType.Air);

					air.Units.Add(a);
				}
				else if (Info.NavalUnitsTypes.Contains(a.Info.Name))
				{
					var ships = GetSquadOfType(SquadCAType.Naval);
					if (ships == null)
						ships = RegisterNewSquad(bot, SquadCAType.Naval);

					ships.Units.Add(a);
				}
				else
					unitsHangingAroundTheBase.Add(a);

				activeUnits.Add(a);
			}

			// Notifying here rather than inside the loop, should be fine and saves a bunch of notification calls
			foreach (var n in notifyIdleBaseUnits)
				n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);

			var protectSq = GetSquadOfType(SquadCAType.Protection);
			if (protectSq != null)
			{
				protectSq.Units = unitsHangingAroundTheBase;
				return;
			}

			protectSq = RegisterNewSquad(bot, SquadCAType.Protection, null);
			protectSq.Units = unitsHangingAroundTheBase;
		}

		void CreateAttackForce(IBot bot)
		{
			// Create an attack force when we have enough units around our base.
			// (don't bother leaving any behind for defense)
			var randomizedSquadSize = Info.SquadSize + World.LocalRandom.Next(Info.SquadSizeRandomBonus);

			if (unitsHangingAroundTheBase.Count >= randomizedSquadSize)
			{
				var attackForce = RegisterNewSquad(bot, SquadCAType.Rush);

				foreach (var a in unitsHangingAroundTheBase)
					attackForce.Units.Add(a);

				unitsHangingAroundTheBase.Clear();
				foreach (var n in notifyIdleBaseUnits)
					n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);
			}
		}

		void ProtectOwn(IBot bot, Actor attacker)
		{
			foreach (SquadCA s in Squads.Where(s => s.IsValid))
			{
				if (s.Type != SquadCAType.Protection)
				{
					if ((s.CenterPosition - attacker.CenterPosition).HorizontalLengthSquared > WDist.FromCells(Info.ProtectUnitScanRadius).LengthSquared)
						continue;
				}

				s.TargetActor = attacker;
			}
		}

		void IBotPositionsUpdated.UpdatedBaseCenter(CPos newLocation)
		{
			initialBaseCenter = newLocation;
		}

		void IBotPositionsUpdated.UpdatedDefenseCenter(CPos newLocation) { }

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e)
		{
			if (!IsPreferredEnemyUnit(e.Attacker))
				return;

			// Protected priority assets, MCVs, harvesters and buildings
			// TODO: Use *CommonNames, instead of hard-coding trait(info)s.
			if (self.Info.HasTraitInfo<HarvesterInfo>() || self.Info.HasTraitInfo<BuildingInfo>() || self.Info.HasTraitInfo<BaseBuildingInfo>())
			{
				foreach (var n in notifyPositionsUpdated)
					n.UpdatedDefenseCenter(e.Attacker.Location);

				ProtectOwn(bot, e.Attacker);
			}
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			return new List<MiniYamlNode>()
			{
				new MiniYamlNode("Squads", "", Squads.Select(s => new MiniYamlNode("Squad", s.Serialize())).ToList()),
				new MiniYamlNode("InitialBaseCenter", FieldSaver.FormatValue(initialBaseCenter)),
				new MiniYamlNode("UnitsHangingAroundTheBase", FieldSaver.FormatValue(unitsHangingAroundTheBase
					.Where(a => !UnitCannotBeOrdered(a))
					.Select(a => a.ActorID)
					.ToArray())),
				new MiniYamlNode("ActiveUnits", FieldSaver.FormatValue(activeUnits
					.Where(a => !UnitCannotBeOrdered(a))
					.Select(a => a.ActorID)
					.ToArray())),
				new MiniYamlNode("AssignRolesTicks", FieldSaver.FormatValue(assignRolesTicks)),
				new MiniYamlNode("protectionForceTicks", FieldSaver.FormatValue(protectionForceTicks)),
				new MiniYamlNode("guerrillaForceTicks", FieldSaver.FormatValue(guerrillaForceTicks)),
				new MiniYamlNode("airForceTicks", FieldSaver.FormatValue(airForceTicks)),
				new MiniYamlNode("navyForceTicks", FieldSaver.FormatValue(navyForceTicks)),
				new MiniYamlNode("groundForceTicks", FieldSaver.FormatValue(groundForceTicks)),
				new MiniYamlNode("MinAttackForceDelayTicks", FieldSaver.FormatValue(minAttackForceDelayTicks)),
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, List<MiniYamlNode> data)
		{
			if (self.World.IsReplay)
				return;

			var initialBaseCenterNode = data.FirstOrDefault(n => n.Key == "InitialBaseCenter");
			if (initialBaseCenterNode != null)
				initialBaseCenter = FieldLoader.GetValue<CPos>("InitialBaseCenter", initialBaseCenterNode.Value.Value);

			var unitsHangingAroundTheBaseNode = data.FirstOrDefault(n => n.Key == "UnitsHangingAroundTheBase");
			if (unitsHangingAroundTheBaseNode != null)
			{
				unitsHangingAroundTheBase.Clear();
				unitsHangingAroundTheBase.AddRange(FieldLoader.GetValue<uint[]>("UnitsHangingAroundTheBase", unitsHangingAroundTheBaseNode.Value.Value)
					.Select(a => self.World.GetActorById(a)).Where(a => a != null));
			}

			var activeUnitsNode = data.FirstOrDefault(n => n.Key == "ActiveUnits");
			if (activeUnitsNode != null)
			{
				activeUnits.Clear();
				activeUnits.AddRange(FieldLoader.GetValue<uint[]>("ActiveUnits", activeUnitsNode.Value.Value)
					.Select(a => self.World.GetActorById(a)).Where(a => a != null));
			}

			var assignRolesTicksNode = data.FirstOrDefault(n => n.Key == "AssignRolesTicks");
			if (assignRolesTicksNode != null)
				assignRolesTicks = FieldLoader.GetValue<int>("AssignRolesTicks", assignRolesTicksNode.Value.Value);

			var protectionForceTicksNode = data.FirstOrDefault(n => n.Key == "protectionForceTicks");
			if (protectionForceTicksNode != null)
				protectionForceTicks = FieldLoader.GetValue<int>("protectionForceTicks", protectionForceTicksNode.Value.Value);

			var guerrillaForceTicksNode = data.FirstOrDefault(n => n.Key == "guerrillaForceTicks");
			if (guerrillaForceTicksNode != null)
				guerrillaForceTicks = FieldLoader.GetValue<int>("guerrillaForceTicks", guerrillaForceTicksNode.Value.Value);

			var airForceTicksNode = data.FirstOrDefault(n => n.Key == "airForceTicks");
			if (airForceTicksNode != null)
				airForceTicks = FieldLoader.GetValue<int>("airForceTicks", airForceTicksNode.Value.Value);

			var navyForceTicksNode = data.FirstOrDefault(n => n.Key == "navyForceTicks");
			if (navyForceTicksNode != null)
				navyForceTicks = FieldLoader.GetValue<int>("navyForceTicks", navyForceTicksNode.Value.Value);

			var groundForceTicksNode = data.FirstOrDefault(n => n.Key == "groundForceTicks");
			if (groundForceTicksNode != null)
				groundForceTicks = FieldLoader.GetValue<int>("groundForceTicks", groundForceTicksNode.Value.Value);

			var minAttackForceDelayTicksNode = data.FirstOrDefault(n => n.Key == "MinAttackForceDelayTicks");
			if (minAttackForceDelayTicksNode != null)
				minAttackForceDelayTicks = FieldLoader.GetValue<int>("MinAttackForceDelayTicks", minAttackForceDelayTicksNode.Value.Value);

			var squadsNode = data.FirstOrDefault(n => n.Key == "Squads");
			if (squadsNode != null)
			{
				Squads.Clear();
				foreach (var n in squadsNode.Value.Nodes)
					Squads.Add(SquadCA.Deserialize(bot, this, n.Value));
			}
		}
	}
}
