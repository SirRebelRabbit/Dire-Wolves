﻿using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace DireWolves
{
    public class DireWolf : Pawn
    {
		public bool isPackLeader;

        public override void Tick()
        {
            base.Tick();
			if (!isPackLeader && Find.TickManager.TicksGame % 60 == 0)
            {
				bool hasLeader = false;
				var packMates = GetPackmates(24f);
				foreach (var packMate in packMates)
                {
					if (packMate.isPackLeader)
                    {
						hasLeader = true;
						break;
                    }
                }
				if (!hasLeader)
                {
					var leader = packMates.AddItem(this).RandomElement();
					leader.isPackLeader = true;
				}
            }
        }
        public float GetHealthPackBonus()
        {
            float num = 1f;
			foreach (var packMate in GetPackmates(24f))
            {
				num += DireWolvesMod.settings.packSizeBonus;
            }
			return num;
        }
		public float GetAttackPackBonus()
		{
			float num = 1f;
			foreach (var packMate in GetPackmates(24f))
			{
				num += DireWolvesMod.settings.packSizeBonus;
			}
			return num;
		}
		private IEnumerable<DireWolf> GetPackmates(float radius)
		{
			if (this.Map != null)
            {
				Room pawnRoom = this.GetRoom();
				List<Pawn> raceMates = this.Map.mapPawns.AllPawnsSpawned;
				for (int i = 0; i < raceMates.Count; i++)
				{
					if (this != raceMates[i] && raceMates[i].def == this.def && raceMates[i].Faction == this.Faction && raceMates[i].Position.InHorDistOf(this.Position, radius) && raceMates[i].GetRoom() == pawnRoom)
					{
						yield return raceMates[i] as DireWolf;
					}
				}
			}
		}

		public bool howlingEnabled;
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
				yield return g;
            }
			if (this.isPackLeader)
            {
				yield return new Command_Toggle
				{
					defaultLabel = "DW.EnableHowling".Translate(),
					defaultDesc = "DW.EnableHowlingDesc".Translate(),
					isActive = () => howlingEnabled,
					toggleAction = delegate
					{
						howlingEnabled = !howlingEnabled;
					}
				};
				if (howlingEnabled)
                {
					yield return new Command_Action
					{
						defaultLabel = "DW.Howl".Translate(),
						defaultDesc = "DW.HowlDesc".Translate(),
						disabled = !CanHowl(),
						action = delegate
						{
							DoHowl();
						}
					};
				}

            }
        }

		private int lastHowlTick;
		public bool CanHowl()
        {
			return this.isPackLeader && howlingEnabled && Find.TickManager.TicksGame >= lastHowlTick + DireWolvesMod.settings.howlingCooldownTicks;
		}

		public void DoHowl()
        {
			var packMates = this.GetPackmates(24);
			foreach (var pawn in GenRadial.RadialDistinctThingsAround(this.Position, this.Map, 15, true).OfType<Pawn>())
            {
				if (pawn.def != this.def)
                {
					if (Rand.Bool)
                    {
						MakeFlee(pawn, this, 15, packMates.Cast<Thing>().ToList());
					}
					else
                    {
						pawn.stances.stunner.StunFor(120, pawn);
                    }
                }
            }
			this.lastHowlTick = Find.TickManager.TicksGame;
        }

		public void MakeFlee(Pawn pawn, Thing danger, int radius, List<Thing> dangers)
		{
			Job job = null;
			IntVec3 intVec;
			if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.Flee)
			{
				intVec = pawn.CurJob.targetA.Cell;
			}
			else
			{
				intVec = CellFinderLoose.GetFleeDest(pawn, dangers, 24f);
			}

			if (intVec == pawn.Position)
			{
				intVec = GenRadial.RadialCellsAround(pawn.Position, radius, radius * 2).RandomElement();
			}
			if (intVec != pawn.Position)
			{
				job = JobMaker.MakeJob(JobDefOf.Flee, intVec, danger);
			}
			if (job != null)
			{
				//Log.Message(pawn + " flee");
				pawn.jobs.TryTakeOrderedJob(job);
			}
		}
		public override void ExposeData()
        {
            base.ExposeData();
			Scribe_Values.Look(ref isPackLeader, "isPackLeader");
			Scribe_Values.Look(ref lastHowlTick, "lastHowlTick");
			Scribe_Values.Look(ref howlingEnabled, "howlingEnabled");
		}
	}
}