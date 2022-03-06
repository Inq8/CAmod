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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.BotModules.Squads;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	public enum SquadCAType { Assault, Air, Rush, Protection, Naval }

	public class SquadCA
	{
		public List<Actor> Units = new List<Actor>();

		// lists used for air squads to determine what members should be doing
		public HashSet<Actor> NewUnits = new HashSet<Actor>();
		public HashSet<Actor> WaitingUnits = new HashSet<Actor>();
		public HashSet<Actor> RearmingUnits = new HashSet<Actor>();

		public SquadCAType Type;

		internal IBot Bot;
		internal World World;
		internal SquadManagerBotModuleCA SquadManager;
		internal MersenneTwister Random;

		internal Target Target;
		internal StateMachineCA FuzzyStateMachine;

		public SquadCA(IBot bot, SquadManagerBotModuleCA squadManager, SquadCAType type)
			: this(bot, squadManager, type, null) { }

		public SquadCA(IBot bot, SquadManagerBotModuleCA squadManager, SquadCAType type, Actor target)
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
				case SquadCAType.Assault:
				case SquadCAType.Rush:
					FuzzyStateMachine.ChangeState(this, new GroundUnitsIdleStateCA(), true);
					break;
				case SquadCAType.Air:
					FuzzyStateMachine.ChangeState(this, new AirIdleStateCA(), true);
					break;
				case SquadCAType.Protection:
					FuzzyStateMachine.ChangeState(this, new UnitsForProtectionIdleState(), true);
					break;
				case SquadCAType.Naval:
					FuzzyStateMachine.ChangeState(this, new NavyUnitsIdleState(), true);
					break;
			}
		}

		public void Update()
		{
			if (IsValid)
				FuzzyStateMachine.Update(this);
		}

		public bool IsValid => Units.Any();

		public Actor TargetActor
		{
			get => Target.Actor;
			set => Target = Target.FromActor(value);
		}

		public bool IsTargetValid => Target.IsValidFor(Units.FirstOrDefault()) && !Target.Actor.Info.HasTraitInfo<HuskInfo>();

		public bool IsTargetVisible => TargetActor.CanBeViewedByPlayer(Bot.Player);

		public WPos CenterPosition { get { return Units.Select(u => u.CenterPosition).Average(); } }

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
			var type = SquadCAType.Rush;
			Actor targetActor = null;

			var typeNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Type");
			if (typeNode != null)
				type = FieldLoader.GetValue<SquadCAType>("Type", typeNode.Value.Value);

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
