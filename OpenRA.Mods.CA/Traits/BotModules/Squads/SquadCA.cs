#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	public enum SquadTypeCA { Assault, Air, Rush, Protection, Naval }

	public class SquadCA
	{
		public List<Actor> Units = new List<Actor>();
		public SquadTypeCA Type;

		internal IBot Bot;
		internal World World;
		internal SquadManagerBotModuleCA SquadManager;
		internal MersenneTwister Random;

		internal Target Target;
		internal StateMachineCA FuzzyStateMachine;

		public SquadCA(IBot bot, SquadManagerBotModuleCA squadManager, SquadTypeCA type)
			: this(bot, squadManager, type, null) { }

		public SquadCA(IBot bot, SquadManagerBotModuleCA squadManager, SquadTypeCA type, Actor target)
		{
			Bot = bot;
			SquadManager = squadManager;
			World = bot.Player.PlayerActor.World;
			Random = World.LocalRandom;
			Type = type;
			Target = Target.FromActor(target);
			FuzzyStateMachine = new StateMachineCA();

			switch (type)
			{
				case SquadTypeCA.Assault:
				case SquadTypeCA.Rush:
					FuzzyStateMachine.ChangeState(this, new GroundUnitsIdleState(), true);
					break;
				case SquadTypeCA.Air:
					FuzzyStateMachine.ChangeState(this, new AirIdleState(), true);
					break;
				case SquadTypeCA.Protection:
					FuzzyStateMachine.ChangeState(this, new UnitsForProtectionIdleState(), true);
					break;
				case SquadTypeCA.Naval:
					FuzzyStateMachine.ChangeState(this, new NavyUnitsIdleState(), true);
					break;
			}
		}

		public void Update()
		{
			if (IsValid)
				FuzzyStateMachine.Update(this);
		}

		public bool IsValid { get { return Units.Any(); } }

		public Actor TargetActor
		{
			get { return Target.Actor; }
			set { Target = Target.FromActor(value); }
		}

		public bool IsTargetValid
		{
			get { return Target.IsValidFor(Units.FirstOrDefault()) && !Target.Actor.Info.HasTraitInfo<HuskInfo>(); }
		}

		public bool IsTargetVisible
		{
			get { return TargetActor.CanBeViewedByPlayer(Bot.Player); }
		}

		public WPos CenterPosition { get { return Units.Select(u => u.CenterPosition).Average(); } }

		public CPos CenterLocation { get { return World.Map.CellContaining(CenterPosition); } }

		void ReflexAvoidance(Actor attacker)
		{
			// Like when you retract your finger when it touches hot stuff,
			// let air untis avoid the attacker very quickly. (faster than flee state's response)
			WVec vec = CenterPosition - attacker.CenterPosition;
			WPos dest = CenterPosition + vec;
			CPos cdest = World.Map.CellContaining(dest);

			foreach (var a in Units)
				Bot.QueueOrder(new Order("Move", a, Target, false));
		}

		internal void Damage(AttackInfo e)
		{
			if (Type == SquadTypeCA.Air)
			{
				// decide flee or retaliate.
				if (AirStateBase.NearToPosSafely(this, CenterPosition))
				{
					TargetActor = e.Attacker;
					FuzzyStateMachine.ChangeState(this, new AirAttackState(), true);
					return;
				}

				// Flee
				ReflexAvoidance(e.Attacker);
				FuzzyStateMachine.ChangeState(this, new AirFleeState(), true);
			}
		}

		public MiniYaml Serialize()
		{
			var nodes = new MiniYaml("", new List<MiniYamlNode>()
			{
				new MiniYamlNode("Type", FieldSaver.FormatValue(Type)),
				new MiniYamlNode("Units", FieldSaver.FormatValue(Units.Select(a => a.ActorID).ToArray())),
			});

			if (Target.Type == TargetType.Actor)
				nodes.Nodes.Add(new MiniYamlNode("Target", FieldSaver.FormatValue(Target.Actor.ActorID)));

			return nodes;
		}

		public static SquadCA Deserialize(IBot bot, SquadManagerBotModuleCA squadManager, MiniYaml yaml)
		{
			var type = SquadTypeCA.Rush;
			Actor targetActor = null;

			var typeNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Type");
			if (typeNode != null)
				type = FieldLoader.GetValue<SquadTypeCA>("Type", typeNode.Value.Value);

			var targetNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Target");
			if (targetNode != null)
				targetActor = squadManager.World.GetActorById(FieldLoader.GetValue<uint>("ActiveUnits", targetNode.Value.Value));

			var squad = new SquadCA(bot, squadManager, type, targetActor);

			var unitsNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Units");
			if (unitsNode != null)
				squad.Units.AddRange(FieldLoader.GetValue<uint[]>("Units", unitsNode.Value.Value)
					.Select(a => squadManager.World.GetActorById(a)));

			return squad;
		}
	}
}
