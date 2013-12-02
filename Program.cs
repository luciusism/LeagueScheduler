using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtensionMethods;
using System.IO;
using System.Data;
using Excel;

namespace LeagueScheduler
{
	class Program
	{
		static void Main(string[] args)
		{
			var errorMessage = string.Empty;
			var start = DateTime.Now;
			try
			{
				

				//var f = new Field() { Availability = new List<DateTimeSlot>() { new DateTimeSlot("10:00 AM", "6:00 PM", "2013-10-11") }, Name = "Field 1" };
				//var minutesPerGame = 120;
				//var slots = f.Availability[0].GetSlots(minutesPerGame);
				//foreach (var s in slots) { Console.WriteLine("Slot Start: " + s.Start.ToShortDateString() + " " + s.Start.ToShortTimeString() 
				//	+ " End: " + s.End.ToShortDateString() + " " + s.End.ToShortTimeString()); }

				var fields = new List<Field>() { new Field() { Name = "Anacostia 2" }, new Field() { Name = "Anacostia 1" }, new Field() { Name = "Sligo 1" } };
				var games = new List<Game>() 
				{ 
					new Game() { Field = fields[1], DateTimeSlot = new DateTimeSlot("2:00 PM", "4:00 PM", "2013-10-10") },
					new Game() { Field = fields[1], DateTimeSlot = new DateTimeSlot("2:00 PM", "4:00 PM", "2013-10-09") },
					new Game() { Field = fields[0], DateTimeSlot = new DateTimeSlot("2:00 PM", "4:00 PM", "2013-10-09") },
					new Game() { Field = fields[0], DateTimeSlot = new DateTimeSlot("2:00 PM", "4:00 PM", "2013-10-10") },
					new Game() { Field = fields[0], DateTimeSlot = new DateTimeSlot("12:00 PM", "2:00 PM", "2013-10-10") }
				};
				foreach (var g in games) { Console.WriteLine(g.DateTimeSlot.Start + " " + g.Field.Name); }
				Console.WriteLine("=========================================");

				games.Sort(
						delegate(Game g1, Game g2)
						{
							// both games the same
							if (g1 == g2) { return 0; }

							// sort by start date/time
							if (g1.DateTimeSlot.Start < g2.DateTimeSlot.Start) { return -1; }
							else if (g1.DateTimeSlot.Start > g2.DateTimeSlot.Start) { return 1; }

							// sort by field
							var g1FieldIndex = fields.FindIndex(x => x == g1.Field);
							var g2FieldIndex = fields.FindIndex(x => x == g2.Field);
							if (g1FieldIndex < g2FieldIndex) { return -1; }
							else if (g1FieldIndex > g2FieldIndex) { return 1; }
							else { return 0; }

							// at this point, throw error since start date/time is the same AND the field is the same
							throw new Exception("Start date/time is the same and the field is tha same");
						}
					);
				foreach (var g in games) { Console.WriteLine(g.DateTimeSlot.Start + " " + g.Field.Name); }
			}
			catch (Exception ex)
			{
				Console.WriteLine("ERROR: " + ex.Message);
			}
			TimeSpan span = DateTime.Now.Subtract(start);
			Console.WriteLine("\nExecution Time: "
				+ span.Minutes + "m "
				+ span.Seconds + "s "
				+ span.Milliseconds + "ms");
			Console.WriteLine("Press any key to continue...");
			Console.ReadLine();
		} // MAIN



		public enum Division
		{
			A, B, C, D
		}
		public class Team
		{
			public string Id { get; set; }
			public string Name { get; set; }
			public Division Division { get; set; }
			public List<Person> Captains { get; set; }
			public League League { get; set; }

			#region Preferences
			public List<DateTime> PreferredByes { get; set; }
			public List<Location> PreferredLocations { get; set; }
			public List<DateTime> PreferredStartTime { get; set; }
			#endregion
			
			// League Scheduler will populate these
			public List<Game> Games { get; set; }

			/// <summary>
			/// Number of preference fails x priority (reversed)
			/// </summary>
			public int PreferenceFailuresWeightedScore { get; set; }
			public List<string> PreferenceSuccesses { get; set; }
			public List<string> PreferenceFailures { get; set; }

			public Team()
			{
				this.Captains = new List<Person>();
				this.PreferredByes = new List<DateTime>();
				this.PreferredLocations = new List<Location>();
				this.PreferredStartTime = new List<DateTime>();
				this.Games = new List<Game>();
			}

			public static int GetPreferenceFailuresWeightedScore(int preferencePriority)
			{
				return 101 - preferencePriority;
			}
		}

		public class Person
		{
			public string Firstname { get; set; }
			public string Lastname { get; set; }
			public string Email { get; set; }
			public string Phone { get; set; }
		}

		public class Location
		{
			public string Name { get; set; }
			public string Address { get; set; }
			public List<Field> Fields { get; set; }

			public Location() { this.Fields = new List<Field>(); }
		}

		public class Field
		{
			public string Name { get; set; }
			public List<DateTimeSlot> Availability { get; set; }
			public Location Location { get; set; }

			public Field() { this.Availability = new List<DateTimeSlot>(); }
		}

		/// <summary>
		/// A range of time, must have the same date, but different start and end time!
		/// </summary>
		public class DateTimeSlot
		{
			public DateTime Start { get; set; }
			public DateTime End { get; set; }

			public DateTimeSlot(string startTime, string endTime, string date)
			{
				this.Start = Convert.ToDateTime(date + " " + startTime);
				this.End = Convert.ToDateTime(date + " " + endTime);
			}

			public DateTimeSlot(DateTime start, DateTime end)
			{
				this.Start = start;
				this.End = end;
				if (start.Date != end.Date) { throw new Exception("DateTime Slot must have the same date"); }
			}

			public List<DateTimeSlot> GetSlots(int minutesPerSlot)
			{
				var slots = new List<DateTimeSlot>();
				if (minutesPerSlot < 1) { throw new Exception("Number of minutes per DateTime slot *must* be greater than 0"); }
				var numberMinutes = this.End - this.Start;
				var numberSlots = Math.Floor(numberMinutes.TotalMinutes / minutesPerSlot);
				for (int i = 0; i < numberSlots; i++)
				{
					slots.Add(new DateTimeSlot(this.Start.AddMinutes(i * minutesPerSlot), this.Start.AddMinutes((i * minutesPerSlot) + minutesPerSlot)));
				}
				return slots;
			}

			///// <summary>
			///// Determine if this timeslot is within passed timeslot.
			///// This timeslot's start time is on or after passed timeslot
			///// This timeSlot's end time is before or after passed timeslot
			///// </summary>
			///// <param name="timeSlotToCompare"></param>
			///// <returns></returns>
			//public bool IsWithin(DateTimeSlot timeSlotToCompare)
			//{
			//	return this.StartTime.TimeOfDay >= timeSlotToCompare.StartTime.TimeOfDay
			//		&& this.EndTime.TimeOfDay <= timeSlotToCompare.EndTime.TimeOfDay
			//		? true : false;

			//	//var startTimeDiff = this.StartTime.TimeOfDay - timeSlotToCompare.StartTime.TimeOfDay;
			//	//var endTimeDiff = this.EndTime.TimeOfDay - timeSlotToCompare.EndTime.TimeOfDay;
			//	//return (startTimeDiff.Minutes >= 0 && endTimeDiff.Minutes <= 0) ? true : false;
			//}
		}

		public class Game
		{
			public Team Team1 { get; set; }
			public Team Team2 { get; set; }
			public Field Field { get; set; }
			public DateTimeSlot DateTimeSlot { get; set; }
			public League League { get; set; }
		}

		public class Clause
		{
			/// <summary>
			/// 1 == absolute
			/// 100 == lowest priority
			/// Higher the priority number, the lower the priority
			/// </summary>
			public byte Priority { get; set; }

			/// <summary>
			/// Determine if game passed satifies this clause
			/// Override for specific clauses
			/// </summary>
			/// <param name="game"></param>
			/// <returns></returns>
			public virtual bool IsSatisfy(Game game) { return true; }
			public virtual bool IsSatisfy(Game game, Team team) { return true; }

			/// <summary>
			/// If true, this clause is a team specific preference. 
			/// If !IsSatisfy, then will count as a preference loss for the team
			/// </summary>
			public bool IsTeamPreference { get; set; }

			/// <summary>
			/// Human readable statement on which preference clause succeded
			/// </summary>
			/// <param name="game"></param>
			/// <returns></returns>
			public virtual string PreferenceSuccessStatement()
			{
				return "Success " + this.GetType();
			}

			/// <summary>
			/// human readble statement on which preference clause failed
			/// </summary>
			/// <param name="game"></param>
			/// <returns></returns>
			public virtual string PreferenceFailStatement()
			{
				return "Failed " + this.GetType();
			}

			public virtual bool IsValid()
			{
				return true;
			}


			protected void finalizeTeamPreferences(Team team, bool isSuccess, string preferenceStatement)
			{
				if (this.IsTeamPreference)
				{
					if (isSuccess)
					{
						// preference success
						team.PreferenceSuccesses.Add(!string.IsNullOrEmpty(preferenceStatement) ? preferenceStatement : this.PreferenceSuccessStatement());
					}
					else
					{
						// preference failure
						team.PreferenceFailures.Add(!string.IsNullOrEmpty(preferenceStatement) ? preferenceStatement : this.PreferenceFailStatement());
						team.PreferenceFailuresWeightedScore += Team.GetPreferenceFailuresWeightedScore(this.Priority);
					}
				}
			}
			
		}
		interface IValidator<T> { bool Validate(T t); }


		/// <summary>
		/// Determine if team gets its bye
		/// </summary>
		public class TeamGetsRequestedBye_Clause : Clause
		{
			public override bool IsSatisfy(Game game, Team team)
			{
				var isSuccess = false;
				
				// no byes prefered
				if (team.PreferredByes == null || team.PreferredByes.Count == 0) { isSuccess = true; }
				else
				{
					// determine if test date matches one of the bye requests
					isSuccess = (team.PreferredByes.Find(x => x.Date == game.DateTimeSlot.Start.Date) == null) ? true : false;
				}

				// finalize preference info
				var statement = isSuccess ? "Successfully received bye requested: " + game.DateTimeSlot.Start.ToShortDateString()
					: "Failed to receive bye requested: " + game.DateTimeSlot.Start.ToShortDateString();
				this.finalizeTeamPreferences(team, isSuccess, statement);

				return isSuccess;
			} 
		}

		/// <summary>
		/// Determine if team gets the field it requested
		/// </summary>
		public class TeamGetsRequestedLocation_Clause : Clause
		{
			public override bool IsSatisfy(Game game, Team team)
			{
				var isSuccess = false;

				// no prefered fields
				if (team.PreferredLocations == null || team.PreferredLocations.Count == 0) { isSuccess = true; }
				else
				{
					// determine if test field matches one of the preferred locations
					isSuccess = team.PreferredLocations.Find(x => x == game.Field.Location) == null ? false : true;
				}

				// finalize preference info
				var statement = isSuccess ? "Successfully received location requested: " + game.Field.Location
					: "Failed to receive location requested " + game.Field.Location;
				this.finalizeTeamPreferences(team, isSuccess, statement);

				return isSuccess;
			}
		}
		/// <summary>
		/// Determine if team gets requested start time
		/// </summary>
		public class TeamGetsRequestedStartTime_Clause : Clause
		{
			public override bool IsSatisfy(Game game, Team team)
			{
				var isSuccess = false;

				// no prefered fields
				if (team.PreferredStartTime == null || team.PreferredStartTime.Count == 0) { isSuccess = true; }
				else
				{
					// determine if test field matches one of the preferred locations
					isSuccess = team.PreferredLocations.Find(x => x == game.Field.Location) == null ? false : true;
				}

				// finalize preference info
				var statement = isSuccess ? "Successfully received location requested: " + game.Field.Location
					: "Failed to receive location requested " + game.Field.Location;
				this.finalizeTeamPreferences(team, isSuccess, statement);

				return isSuccess;
			}
		}

		public class OpponentClause : Clause
		{
			public override bool IsSatisfy(Game game, Team t)
			{
				// not playing against self
				return true;
			}

		}


		//public class TimeSlotClause : Clause
		//{
		//	public List<DateTimeSlot> PreferedTimeSlots { get; set; }
		//	public TimeSlotClause() { this.PreferedTimeSlots = new List<DateTimeSlot>(); }
		//	public override bool IsSatisfy(Game game)
		//	{
		//		// no preferred timeslots
		//		if (PreferedTimeSlots == null || PreferedTimeSlots.Count == 0) { return true; }

		//		// determine if test timeslot matches preferred time slots
		//		return PreferedTimeSlots.Find(x => game.TimeSlot.IsWithin(x)) == null ? false : true;
		//	}
		//}


		public class League
		{
			#region League Contants
			public const int NumberTests = 1000;
			public int NumberGamesPerTeamMin { get; set; }
			public int NumberGamesPerTeamMax { get; set; }
			public int MinutesPerGame { get; set; }
			public int PreferenceByePriority { get; set; }
			public int PreferenceLocationPriority { get; set; }
			public int PreferenceStartTimePriority { get; set; }
			#endregion

			public string Name { get; set; }
			public List<Team> Teams { get; set; }
			public List<Field> Fields { get; set; }
			public List<Game> Games { get; set; }
			public List<Clause> Clauses { get; set; }
			public List<DateTime> GameDays { get; set; }

			public League()
			{
				this.Teams = new List<Team>();
				this.Fields = new List<Field>();
				this.Games = new List<Game>();
				this.Clauses = new List<Clause>();
				this.GameDays = new List<DateTime>();
			}

			public enum Settings
			{
				Name,
				Year,
				Season,
				GameDays,
				MinutesPerGame,
				NumberGamesPerTeamMin,
				NumberGamesPerTeamMax
			}

			/// <summary>
			/// load league settings from excel
			/// </summary>
			/// <param name="filePath"></param>
			/// <param name="league"></param>
			public static void Load(string filePath, out League league)
			{
				DataSet ds = null;
				try
				{
					// open excel file
					var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
					IExcelDataReader excelReader = ExcelReaderFactory.CreateBinaryReader(stream);

					// convert to dataset
					excelReader.IsFirstRowAsColumnNames = true;
					ds = excelReader.AsDataSet();

					// close
					excelReader.Close();

					// create league
					league = new League();

					foreach (DataRow r in ds.Tables["league"].Rows)
					{
						var p = r["ID"].ToString();
						// Leage Name
						if (Settings.Name.ToString() == p)
						{
							league.Name = r["Value"].ToString();
						}
						// Game Days
						else if (Settings.GameDays.ToString() == p)
						{
							var gameDaysAsString = r["value"].ToString();
							if (string.IsNullOrEmpty(gameDaysAsString)) { throw new Exception("No game days defined"); }
							gameDaysAsString = gameDaysAsString.Replace("\r\n", ",").Replace("\r", ",").Replace("\n", ",");
							if (gameDaysAsString.Substring(gameDaysAsString.Length - 1) == ",") { gameDaysAsString = gameDaysAsString.Substring(0, gameDaysAsString.Length - 1); }
							var gameDays = gameDaysAsString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
							if (gameDays == null || gameDays.Length == 0) { throw new Exception("Failed to convert values into game days"); }

							foreach (var d in gameDays)
							{
								DateTime gameDay = DateTime.MinValue; 
								DateTime.TryParse(d.Trim(), out gameDay);
								if (gameDay == DateTime.MinValue) { throw new Exception("Malformed date: " + d); }
								league.GameDays.Add(gameDay);
							}
						}
						// Minutes per game
						else if (Settings.MinutesPerGame.ToString() == p)
						{
							int minutesPerGame = 0;  
							Int32.TryParse(r["value"].ToString(), out minutesPerGame);
							if (minutesPerGame == 0) { throw new Exception("Either failed to determine minutes per game, or was set to 0"); }
							league.MinutesPerGame = minutesPerGame;
						}
						// NumberGamesPerTeamMin
						else if (Settings.NumberGamesPerTeamMin.ToString() == p)
						{
							if (string.IsNullOrEmpty(r["value"].ToString())) { throw new Exception("Min number games per team not defined"); }
							int numberGamesPerTeamMin = 0;
							Int32.TryParse(r["value"].ToString(), out numberGamesPerTeamMin);
							if (numberGamesPerTeamMin == 0) { throw new Exception("Min number of games per team not defined"); }
							league.NumberGamesPerTeamMin = numberGamesPerTeamMin;
						}
						// NumberGamesPerTeamMax
						else if (Settings.NumberGamesPerTeamMax.ToString() == p)
						{
							if (string.IsNullOrEmpty(r["value"].ToString())) { throw new Exception("Max number games per team not defined"); }
							int numberGamesPerTeamMax = 0;
							Int32.TryParse(r["value"].ToString(), out numberGamesPerTeamMax);
							if (numberGamesPerTeamMax == 0) { throw new Exception("Max number of games per team not defined"); }
							league.NumberGamesPerTeamMax = numberGamesPerTeamMax;
						}
					} // foreach row in league

					// fields
					foreach (DataRow r in ds.Tables["fields"].Rows)
					{
						var location = r["Location"].ToString();
						if (string.IsNullOrEmpty(location)) { continue; }
						var region = r["Region"].ToString();
						if (string.IsNullOrEmpty(region)) { throw new Exception("Region is undefined for location: " + location); }
						var name = r["Name"].ToString();
						if (string.IsNullOrEmpty(name)) { throw new Exception("Name undefined for location: " + location); }
						var days = r["Days"].ToString();
						var startTime = r["Start Time"].ToString();
						if (string.IsNullOrEmpty(startTime)) { throw new Exception("Start time undefined for location: " + location); }
						var endTime = r["End Time"].ToString();
						if (string.IsNullOrEmpty(endTime)) { throw new Exception("End time undefined for location: " + location); }
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
					league = null;
				}
			}

			public bool ScheduleGames()
			{
				var isSuccess = true;
				try
				{
					// validate
					if (Teams == null || Teams.Count < 2) { throw new Exception("League must have more than 1 team"); }
					if (GameDays == null || GameDays.Count < 1) { throw new Exception("League must have more than 0 game days"); }


					//////////////////////////////////////////////////////////////////
					//////////////////////////////////////////////////////////////////
					// create games collection (no teams yet)
					this.Games = new List<Game>();
					foreach (var d in this.GameDays)
					{
						foreach (var f in this.Fields)
						{
							if (f.Availability == null || f.Availability.Count == 0) { continue; }
							foreach (var a in f.Availability)
							{
								if (a.Start.Date == d.Date)
								{
									// found a field with availability on this game day
									// now devide the field's availability into time slots
									var slots = a.GetSlots(MinutesPerGame);
									foreach (var s in slots) { this.Games.Add(new Game() { DateTimeSlot = s, Field = f, League = this }); }
								}
							}
						}
					}

					//////////////////////////////////////////////////////////////////
					//////////////////////////////////////////////////////////////////
					// sort games: by start time, then by field order
					this.Games.Sort(
						delegate(Game g1, Game g2)
						{
							// both games the same
							if (g1 == g2) { return 0; }

							// sort by start date/time
							if (g1.DateTimeSlot.Start < g2.DateTimeSlot.Start) { return -1; }
							else if (g1.DateTimeSlot.Start > g2.DateTimeSlot.Start) { return 1; }

							// sort by field
							var g1FieldIndex = this.Fields.FindIndex(x => x == g1.Field);
							var g2FieldIndex = this.Fields.FindIndex(x => x == g2.Field);
							if (g1FieldIndex < g2FieldIndex) { return -1; }
							else if (g1FieldIndex > g2FieldIndex) { return 1; }
							else { return 0; }

							// at this point, throw error since start date/time is the same AND the field is the same
							throw new Exception("Start date/time is the same and the field is tha same");
						}
					);



					//////////////////////////////////////////////////////////////////
					//////////////////////////////////////////////////////////////////
					// Default Clauses
					var clauses = new List<Clause>();
					
					// sort clauses
					clauses.Sort(delegate (Clause c1, Clause c2) { return c1.Priority.CompareTo(c2.Priority); });


					//////////////////////////////////////////////////////////////////
					//////////////////////////////////////////////////////////////////
					// loop through games and assign teams using SAT clauses
					var fitnessScore = 0;
					var index1 = 0;
					while (index1 < NumberTests)
					{
						index1++;
						
						// randomize teams
						// everytime loop, get new shuffle of teams, and hopfully a better "fit"
						this.Teams.Shuffle();


						// loop through games
						foreach (var g in this.Games)
						{
							foreach (var t1 in this.Teams)
							{
								// convert preferences to SAT clauses
								if (t1.PreferredByes != null && t1.PreferredByes.Count > 0)
								{

								}
							}
						}

					}
						


					// time slots




					// randomize the list of teams, team 1 becomes the "hub" team
					
					// sort game days
					GameDays.OrderBy(x => x.Date);

					// loop through game days
					var gameDaysIndex = 1;
					foreach (var day in GameDays)
					{
						// loop through teams, remove those one bye
						var teamsForThisGameDay = new List<Team>();
						foreach (var team in Teams)
						{
							if (team.PreferredByes.Find(x => x.Date == day.Date) != null)
							{
								// this team has a bye for this game day, skip
								continue;
							}
							else
							{
								teamsForThisGameDay.Add(team);
							}
						}

						// if remaining teams playing this game day is odd, remove last team
						if (teamsForThisGameDay.Count % 2 != 0) { teamsForThisGameDay.RemoveAt(teamsForThisGameDay.Count - 1); }

						// loop through 

						gameDaysIndex++;
					} // FOREACH GAME DAYS


					// build clauses

					// start the satisfiability tests

				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
					isSuccess = false;
				}
				return isSuccess;
			}

			private bool IsSatisfy(Game game, List<Clause> clauses, byte maxPriorityToSatisfy)
			{
				try
				{
					// validate
					if (maxPriorityToSatisfy < 1) { throw new Exception("a Priority of 1 is absolute. Anthing lower is unexpected"); }

					// no clauses passed, assume satisfy (default)
					if (clauses == null || clauses.Count == 0) { return true; }

					// order clauses by priority. Lower the priority, the more important. 1 = absolute
					clauses.OrderBy(x => x.Priority);

					foreach (var c in clauses)
					{
						// exit if exceed max priority to satisfy
						if (c.Priority > maxPriorityToSatisfy) { break; }

						// if fail to satisy, break & return false
						if (!c.IsSatisfy(game)) { return false; }
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
					return false;
				}
				return true;
			}
		}

		
	}
}



namespace ExtensionMethods
{
	public static class Extensions
	{
		// http://stackoverflow.com/questions/273313/randomize-a-listt-in-c-sharp
		public static void Shuffle<T>(this IList<T> list)
		{
			Random rng = new Random();
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = rng.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}
	}
}
