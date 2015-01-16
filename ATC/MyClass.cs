using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

using KSP;
using UnityEngine;

public enum ATCclass {
	Tower,
	Approach,
	Ground,
	Central,
	Space_Center
}
public enum FlightPlanType {
	None,
	LowAltitude,
	HighAltitude,
	Suborbital,
	Orbital
}

namespace ATC
{

	public class Section {
		public String name;
		public float frequency;
		public ATCclass type;
	}
	public class Runway {
		public string name;
		public string altname = "";
		public int heading = 0;
		public float tllat;
		public float tllon;
		public float trlat;
		public float trlon;
		public float bllat;
		public float bllon;
		public float brlat;
		public float brlon;
		public bool isOnRunway(double lat, double lon) {
			// TODO
			return true;
		}
	}
	public class Station
	{
		public String name;
		public Section tower;
		public Section ground;
		public Section approach;
		public Section central;
		public Section space_center;
		public Runway runway;
		public double latitude;
		public double longitude;
		public string code;
		public Station (String station_name) {
			name = station_name;
//			latitude = -0.062;
//			longitude = 285.367;
//			tower = new Section ();
//			tower.name = "KSC Tower";
//			tower.frequency = 118.2f;
//			tower.type = ATCclass.Tower;
//			ground = new Section ();
//			ground.name = "KSC Ground";
//			ground.frequency = 118.9f;
//			ground.type = ATCclass.Ground;
//			approach = new Section ();
//			approach.name = "KSC Approach";
//			approach.frequency = 118.5f;
//			approach.type = ATCclass.Approach;
//			central = new Section ();
//			central.name = "KSC Central";
//			central.frequency = 119.1f;
//			central.type = ATCclass.Central;
//			space_center = new Section ();
//			space_center.name = "KSC";
//			space_center.frequency = 117.9f;
//			space_center.type = ATCclass.Space_Center;
		}
		public double distance() {
			return Vector3.Distance (FlightGlobals.getMainBody ().GetRelSurfacePosition (latitude, longitude, 0),
			                        FlightGlobals.getMainBody ().GetRelSurfacePosition (FlightGlobals.ActiveVessel.latitude, FlightGlobals.ActiveVessel.longitude, FlightGlobals.ActiveVessel.altitude));
		}
		public int heading () {
			// 2 is station
			double φ1 = (FlightGlobals.ActiveVessel.latitude / 180) * Math.PI;
			double φ2 = (latitude / 180) * Math.PI;
			double Δλ = ((longitude - FlightGlobals.ActiveVessel.longitude) / 180) * Math.PI;
//			return (int)((Math.Atan2 (Math.Sin (longitude - FlightGlobals.ActiveVessel.longitude) * Math.Cos (latitude),
//			          Math.Cos (FlightGlobals.ActiveVessel.latitude) * Math.Sin (latitude) - Math.Sin (FlightGlobals.ActiveVessel.latitude) * Math.Cos (latitude) * Math.Cos (longitude - FlightGlobals.ActiveVessel.longitude))/Math.PI*180 + 360)%360);
			return (int)((Math.Atan2 (Math.Sin (Δλ) * Math.Cos (φ2), Math.Cos (φ1) * Math.Sin (φ2) - Math.Sin (φ1) * Math.Cos (φ2) * Math.Cos (Δλ)) / Math.PI * 180 + 360) % 360);
		}
		public string direction () {
			int h = (heading ()+180) % 360;
			if (h > 338 || h < 22) {
				return "north";
			} else if (h < 67) {
				return "northeast";
			} else if (h < 112) {
				return "east";
			} else if (h < 158) {
				return "southeast";
			} else if (h < 202) {
				return "south";
			} else if (h < 248) {
				return "southwest";
			} else if (h < 292) {
				return "west";
			} else {
				return "northwest";
			}
		}
	}

	public class SortByDistance : IComparer<Station>
	{
		public int Compare(Station x, Station y)
		{
			int compareDistance = x.distance().CompareTo(y.distance());
			return compareDistance;
		}
	}

	public class FlightPlan
	{
		public FlightPlanType type;
		public Station destination;
		public int altitude;

		public FlightPlan () {
			type = FlightPlanType.None;
		}
	}

	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class MyClass : MonoBehaviour
	{
		private static Rect MainGUI = new Rect(100, 50, 450, 350);
		public bool isWindowOpen = true; //Value for GUI window open
		public double stationDistance;
		public string Callsign = "NKSP";

		private Station station;// = new Station("KSC");
		private Section section;
		
		public string message1 = "";
		public string message2 = "";
		public string message3 = "";
		public string message4 = "";
		private bool who1 = true;
		private bool who2 = true;
		private bool who3 = true;
		private bool who4 = true;

		private string actionToRun = "";
		private int ticksRemaining = 0;
		private int timer = 0;

		public bool stationContacted = false;
		public bool flightPermission = false;
		public bool landingPermission = false;
		public bool hasBeenToSpace = false;
		public bool transferring = false;
		public FlightPlan plan = new FlightPlan ();
		
		GUIStyle yellowStyle;
		GUIStyle whiteStyle;

		private bool planning = false;

		private ConfigNode configFile;
		private List<Station> stations = new List<Station>();
		private List<Station> airports = new List<Station>();
		public float glideAltitude;

		private string currentNameText = "";
		private string currentCodeText = "";
		private Vector2 scrollPosition = new Vector2 (0, 0);

		private IComparer<Station> distanceComparer = new SortByDistance ();

		int windGUIID;

		void Awake()
		{
			//GUI id hashcodes
			windGUIID = Guid.NewGuid ().GetHashCode ();
//			airports.Add (station);

//			Station island = new Station ("Eastern Island");
//			island.tower.name = "Eastern Island Tower";
//			island.tower.frequency = 125.3f;
//			island.ground = null;
//			island.latitude = -1.5226;
//			island.longitude = 288.0881;
//			airports.Add (island);

			configFile = ConfigNode.Load (KSPUtil.ApplicationRootPath + "GameData/ATC/stations.cfg");

			foreach (ConfigNode stationSettings in configFile.GetNodes("STATION")) {
				Station s = new Station (stationSettings.GetValue ("name"));
				s.latitude = float.Parse (stationSettings.GetValue ("latitude"));
				s.longitude = float.Parse (stationSettings.GetValue ("longitude"));
				stations.Add (s);
				s.code = stationSettings.GetValue("code");
				if (bool.Parse (stationSettings.GetValue ("airport"))) {
					airports.Add (s);
					s.runway = new Runway ();
					s.runway.name = stationSettings.GetValue("runway");
					if (stationSettings.HasValue("altrunway")) {
						s.runway.altname = stationSettings.GetValue("altrunway");
						s.runway.heading = int.Parse(stationSettings.GetValue("runwayheading"));
					}
				}
				if (stationSettings.HasNode ("Tower")) {
					ConfigNode tower = stationSettings.GetNode ("Tower");
					s.tower = new Section ();
					s.tower.name = tower.GetValue ("name");
					s.tower.frequency = float.Parse (tower.GetValue ("frequency"));
					s.tower.type = ATCclass.Tower;
				}
				if (stationSettings.HasNode ("Ground")) {
					ConfigNode ground = stationSettings.GetNode ("Ground");
					s.ground = new Section ();
					s.ground.name = ground.GetValue ("name");
					s.ground.frequency = float.Parse (ground.GetValue ("frequency"));
					s.ground.type = ATCclass.Ground;
				}
				if (stationSettings.HasNode ("Approach")) {
					ConfigNode approach = stationSettings.GetNode ("Approach");
					s.approach = new Section ();
					s.approach.name = approach.GetValue ("name");
					s.approach.frequency = float.Parse (approach.GetValue ("frequency"));
					s.approach.type = ATCclass.Approach;
				}
				if (stationSettings.HasNode ("Central")) {
					ConfigNode central = stationSettings.GetNode ("Central");
					s.central = new Section ();
					s.central.name = central.GetValue ("name");
					s.central.frequency = float.Parse (central.GetValue ("frequency"));
					s.central.type = ATCclass.Central;
				}
				if (stationSettings.HasNode ("Space_Center")) {
					ConfigNode space_center = stationSettings.GetNode ("Space_Center");
					s.space_center = new Section ();
					s.space_center.name = space_center.GetValue ("name");
					s.space_center.frequency = float.Parse (space_center.GetValue ("frequency"));
					s.space_center.type = ATCclass.Space_Center;
				}
			}




//			RenderingManager.AddToPostDrawQueue(0, OnDraw);
		}

		float VesselHeading() {
			Vector3d CoM, north, up;
			Quaternion rotationSurface;
			CoM = FlightGlobals.ActiveVessel.findWorldCenterOfMass();
			up = (CoM - FlightGlobals.ActiveVessel.mainBody.position).normalized;
			north = Vector3d.Exclude(up, (FlightGlobals.ActiveVessel.mainBody.position + FlightGlobals.ActiveVessel.mainBody.transform.up *
			                              (float)FlightGlobals.ActiveVessel.mainBody.Radius) - CoM).normalized;
			rotationSurface = Quaternion.LookRotation(north, up);
			Quaternion vesselOrientation = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) *
			                          Quaternion.Inverse(FlightGlobals.ActiveVessel.GetTransform().rotation) * rotationSurface);
			return vesselOrientation.eulerAngles.y;
		}
		float RelativeHeading () {
			return (plan.destination.heading () - VesselHeading () + 180) % 360 - 180;
		}

		//Called after Update()
		void LateUpdate()
		{
		}
		/*
* Called next.
*/
		void Start()
		{
		}
		/*
* Called every frame
*/
		void Update()
		{
		}

		void FixedUpdate()
		{
//			if (!stationContacted) {
//				contactStation ();
//				stationContacted = true;
//			}
		}

		void OnGUI()
		{
			if (FlightGlobals.currentMainBody.name != "Kerbin" || FlightGlobals.ActiveVessel.GetCrewCount() < 1)
				return;
			
			if (station == null) {
				if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.LANDED || FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH || FlightGlobals.ActiveVessel.situation == Vessel.Situations.SPLASHED) {
					station = getTower ();
					if (station.ground != null && station.distance() < 4000) {
						section = station.ground;
					} else {
						section = station.tower;
						stationContacted = true;
					}
				} else if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.FLYING) {
					flightPermission = true;
					if (getTower().distance()<10000) {
						station = getTower();
						section = station.tower;
					} else if (getApproach().distance()<30000 & FlightGlobals.ActiveVessel.altitude<15000) {
						station = getApproach();
						section = station.approach;
					} else {
						station = getCentral();
						section = station.central;
					}
				} else {
					hasBeenToSpace = true;
					flightPermission = true;
					station = getSpaceCenter();
					section = station.space_center;
				}
			}

			string statusString = "";
			if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.FLYING && section.type == ATCclass.Tower && landingPermission) {
				statusString = "<cleared for landing>";
			} else if ((FlightGlobals.ActiveVessel.situation == Vessel.Situations.LANDED || FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH) && section.type == ATCclass.Tower && flightPermission) {
				statusString = "<cleared for takeoff>";
			} else if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.FLYING && (plan.type == FlightPlanType.LowAltitude ||
			                                                                                plan.type == FlightPlanType.HighAltitude ||
			                                                                                (plan.type == FlightPlanType.Suborbital && hasBeenToSpace && FlightGlobals.ActiveVessel.altitude < 50000)) && plan.destination != null) {
				statusString = "<maintain " + Math.Min (plan.altitude, glideAltitude).ToString () + " heading " + plan.destination.heading ().ToString () + "°>";
			} else if (plan.type == FlightPlanType.Suborbital) {
				statusString = "<suborbital flight>";
			} else if (plan.type == FlightPlanType.Orbital) {
				statusString = "<ascend to orbit>";
			} else if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.FLYING) {
				statusString = "<flying>";
			} else if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.LANDED) {
				statusString = "<landed>";
			} else if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.SPLASHED) {
				statusString = "<splashed down>";
			} else if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.SUB_ORBITAL) {
				statusString = "<suborbital flight>";
			} else if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.ORBITING || FlightGlobals.ActiveVessel.situation == Vessel.Situations.ESCAPING) {
				statusString = "<orbiting>";
			} else if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH) {
				statusString = "<ready to launch>";
			} else {
				statusString = "";
			}
			MainGUI = GUI.Window(windGUIID, MainGUI, OnWindow, section.frequency+" - ["+station.code+"] "+section.name + "  " + statusString);
			yellowStyle = new GUIStyle(GUI.skin.label);
			whiteStyle = new GUIStyle(GUI.skin.label);
			yellowStyle.normal.textColor = yellowStyle.focused.textColor = Color.yellow;
			whiteStyle.normal.textColor = whiteStyle.focused.textColor = Color.white;
		}

		Station getTower() {
			double mindist = 12000000;
			Station tower = null;
			foreach (Station s in airports) {
				if (s.distance () < mindist) {
					tower = s;
					mindist = s.distance ();
				}
			}
			return tower;
		}
		Station getApproach() {
			double mindist = 12000000;
			Station approach = null;
			foreach (Station s in stations) {
				if (s.approach != null) {
					if (s.distance () < mindist) {
						approach = s;
						mindist = s.distance ();
					}
				}
			}
			return approach;
		}
		Station getCentral() {
			double mindist = 12000000;
			Station central = null;
			foreach (Station s in stations) {
				if (s.central != null) {
					if (s.distance () < mindist) {
						central = s;
						mindist = s.distance ();
					}
				}
			}
			return central;
		}
		Station getSpaceCenter() {
			double mindist = 12000000;
			Station space_center = null;
			foreach (Station s in stations) {
				if (s.space_center != null) {
					if (s.distance () < mindist) {
						space_center = s;
						mindist = s.distance ();
					}
				}
			}
			return space_center;
		}

		void doFlightPlanGUI (bool nearest_only = false) {
			if (planning) {
				GUILayout.Label("Select destination:");
				if (!nearest_only) {
					GUILayout.BeginHorizontal();
					GUILayout.Label("Name:",GUILayout.ExpandWidth(false));
					currentNameText = GUILayout.TextField(currentNameText, GUILayout.MinWidth(30.0F), GUILayout.ExpandWidth(true));
					GUILayout.Label(" | Airport code:", GUILayout.ExpandWidth(false));
					currentCodeText = GUILayout.TextField(currentCodeText, GUILayout.Width(40.0F)).ToUpper();
					GUILayout.EndHorizontal();
					if (currentCodeText.Length>3) {
						currentCodeText = currentCodeText.Substring(0,3);
					}
				} else {
					if (GUILayout.Button("Cancel")) {
						plan.type = FlightPlanType.None;
						planning = false;
					}
				}
				airports.Sort(distanceComparer);
				scrollPosition = GUILayout.BeginScrollView(scrollPosition);
				foreach(Station s in airports) {
					if (nearest_only && s.distance() > 250000) break;
					if ((currentCodeText.Length>0 && s.code.Contains(currentCodeText)) ||
					    (currentNameText.Length>0 && s.name.ToLower().Contains(currentNameText.ToLower())) ||
					    (currentCodeText.Length==0 && currentNameText.Length==0)) {
						if (GUILayout.Button(s.name+" ("+s.code+")")) {
							plan.destination = s;
							planning = false;
							if (plan.type == FlightPlanType.LowAltitude) {
								postMessage(section.name+", "+Callsign+" requests flight plan, destination "+plan.destination.name+", low altitude.", true);
							} else if (plan.type == FlightPlanType.HighAltitude) {
								postMessage(section.name+", "+Callsign+" requests flight plan, destination "+plan.destination.name+" via high altitude airways.", true);
							} else if (plan.type == FlightPlanType.Suborbital) {
								postMessage(section.name+", "+Callsign+" requests exo-atmospheric flight to "+plan.destination.name+".", true);
							}
							startTimeout("SFP",500);
						}
					}
				}
				GUILayout.EndScrollView();
			} else {
				if (nearest_only) {
					if (GUILayout.Button("Nearest airport list")) {
						plan.type = FlightPlanType.LowAltitude;
						plan.altitude = 5000;
						planning = true;
					}
				} else {
					if (GUILayout.Button("Request flight plan (low altitude airways)")) {
						plan.type = FlightPlanType.LowAltitude;
						plan.altitude = 5000;
						planning = true;
					}
					if (GUILayout.Button("Request flight plan (high altitude airways)")) {
						plan.type = FlightPlanType.HighAltitude;
						plan.altitude = 20000;
						planning = true;
					}
					if (GUILayout.Button("Request flight plan (suborbital hop)")) {
						plan.type = FlightPlanType.Suborbital;
						plan.altitude = 20000;
						planning = true;
					}
					if (GUILayout.Button("Request flight plan (ascent to orbit)")) {
						postMessage(section.name+", "+Callsign+" requests ascent to orbit.", true);
						plan.type = FlightPlanType.Orbital;
						startTimeout("SFP",500);
					}
				}

			}
		}

		void OnWindow(int windowId)
		{

			if (ticksRemaining > 0) {
				ticksRemaining--;
				if (ticksRemaining == 0) {
					runAction (actionToRun);
				}
			}
			if (isWindowOpen == true)
			{
				if (FlightGlobals.ActiveVessel.altitude > 69200) {
					hasBeenToSpace = true;
				}
				GUILayout.BeginVertical();
				GUILayout.Label(message4, who4 ? whiteStyle : yellowStyle);
				GUILayout.Label(message3, who3 ? whiteStyle : yellowStyle);
				GUILayout.Label(message2, who2 ? whiteStyle : yellowStyle);
				GUILayout.Label(message1, who1 ? whiteStyle : yellowStyle);


//				GUILayout.Label("Lat: "+FlightGlobals.ActiveVessel.latitude.ToString());
//				GUILayout.Label("Lon: "+FlightGlobals.ActiveVessel.longitude.ToString());
				
				Station towerStation = getTower();
				Section tower = towerStation.tower;
				Station approachStation = getApproach();
				Section approach = approachStation.approach;
				Station centralStation = getCentral();
				Section central = centralStation.central;
				Station spaceCenterStation = getSpaceCenter();
				Section spaceCenter = spaceCenterStation.space_center;
				if (plan.destination != null) {
					glideAltitude = Math.Max(((int)(plan.destination.distance()*0.1*0.001))*1000,1000);
				}
				if (ticksRemaining==0) {
					if (section.type==ATCclass.Tower) {
						if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.FLYING) {
							if (!flightPermission) {
								postMessage(Callsign+", you were not cleared to take off.", false);
								if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER) {
									Reputation.Instance.AddReputation(-50,TransactionReasons.Any);
								}
								flightPermission = true;
							} else {
								if (station.distance()>10100) {
									if (approachStation.distance()>30000 || FlightGlobals.ActiveVessel.altitude>15000) {
										if (!transferring) {
											postMessage(Callsign+", contact "+central.name+" on "+central.frequency+".", false);
											startTimeout("NUL", 200);
											transferring = true;
										} else {
											if (GUILayout.Button("Tune "+central.name+" on "+central.frequency.ToString())) {
												postMessage("Going to "+central.frequency+", "+Callsign+".", true);
												section = central;
												transferring = false;
												stationContacted = false;
											}
										}
									} else {
										if (!transferring) {
											postMessage(Callsign+", contact "+approach.name+" on "+approach.frequency+".", false);
											startTimeout("NUL", 200);
											transferring = true;
										} else {
											if (GUILayout.Button("Tune "+approach.name+" on "+approach.frequency.ToString())) {
												station = approachStation;
												section = approach;
												postMessage("Going to "+approach.frequency+", "+Callsign+".", true);
												transferring = false;
												stationContacted = false;
											}
										}
									}
								} else {
									if (!stationContacted) {
										if (GUILayout.Button("Contact "+section.name)) {
											if (!(plan.type == FlightPlanType.None || plan.type == FlightPlanType.Orbital) && station==plan.destination) {
												postMessage(section.name+", "+Callsign+" is "+((int)((station.distance()+500)/1000)).ToString()+" kilometers "+station.direction()+", inbound for landing.", true);
											} else {
												postMessage(section.name+", this is "+Callsign+" with you, "+(1000*(int)((FlightGlobals.ActiveVessel.altitude+500)/1000)).ToString()+".",true);
											}
											startTimeout("CON", 500);
											stationContacted = true;
										}
									} else {
										if (!landingPermission) {
											if (timer==0) {
												if (plan.type == FlightPlanType.LowAltitude || plan.type == FlightPlanType.HighAltitude || (plan.type == FlightPlanType.Suborbital && hasBeenToSpace)) {
													if (Math.Abs(RelativeHeading())>5) {
														if (Math.Min(plan.altitude,glideAltitude) - FlightGlobals.ActiveVessel.altitude > 300) {
															postMessage(Callsign+", turn "+(RelativeHeading()>0?"right":"left")+" heading "+plan.destination.heading().ToString()+", climb and maintain "+Math.Min(plan.altitude,glideAltitude)+".", false);
														} else if (Math.Min(plan.altitude,glideAltitude) - FlightGlobals.ActiveVessel.altitude < -300) {
															postMessage(Callsign+", turn "+(RelativeHeading()>0?"right":"left")+" heading "+plan.destination.heading().ToString()+", descend and maintain "+Math.Min(plan.altitude,glideAltitude)+".", false);
														} else {
															postMessage(Callsign+", turn "+(RelativeHeading()>0?"right":"left")+" heading "+plan.destination.heading().ToString()+".",false);
														}
													} else {
														if (Math.Min(plan.altitude,glideAltitude) - FlightGlobals.ActiveVessel.altitude > 300) {
															postMessage(Callsign+", climb and maintain "+Math.Min(plan.altitude,glideAltitude)+".", false);
														} else if (Math.Min(plan.altitude,glideAltitude) - FlightGlobals.ActiveVessel.altitude < -300) {
															postMessage(Callsign+", descend and maintain "+Math.Min(plan.altitude,glideAltitude)+".", false);
														} else {
														}
													}
												}
												timer = 3000;
											} else {
												timer -= 1;
											}
											if (plan.type == FlightPlanType.None || station == plan.destination ) {
												if (GUILayout.Button("Request landing clearance")) {
													postMessage(section.name+", "+Callsign+" is inbound at "+(1000*(int)((FlightGlobals.ActiveVessel.altitude+500)/1000)).ToString()+", request clearance for landing.", true);
													startTimeout("RLC", 500);
													landingPermission = true;
												}
												if (GUILayout.Button("Request landing clearance (remain in pattern)")) {
													postMessage(section.name+", "+Callsign+" is inbound at "+(1000*(int)((FlightGlobals.ActiveVessel.altitude+500)/1000)).ToString()+", request clearance for touch-and-go.", true);
													startTimeout("RTG", 500);
													landingPermission = true;
												}
												if (plan.type != FlightPlanType.None && station == plan.destination && station.distance() < 3500) {
													postMessage(Callsign + ", cleared for landing, "+((station.runway.altname!="" && (station.heading()-station.runway.heading+630)%360<180)?station.runway.altname:station.runway.name)+".", false);
													startTimeout("NUL", 200);
													landingPermission = true;
												}
											}
										} else {
											if (GUILayout.Button("Announce go-around")) {
												postMessage(section.name+", "+Callsign+" is going around.", true);
												startTimeout("AGA", 200);
											}
											if (GUILayout.Button("Cancel landing intentions")) {
												postMessage(section.name+", "+Callsign+" is diverting.", true);
												landingPermission = false;
												plan.type = FlightPlanType.None;
												startTimeout("CLI", 200);
											}
										}
									}
								}
							}
						} else {
							if (!flightPermission) {
								if (GUILayout.Button("Request takeoff clearance")) {
									contactStation();
									flightPermission = true;
									landingPermission = false;
									timer = 700;
									startTimeout("RTC", 250);
								}
							}
							if (station.ground==null && plan.type == FlightPlanType.None && !planning) {
								doFlightPlanGUI();
							}
						}
						if (plan.type != FlightPlanType.None && plan.destination != null) {
							if ((FlightGlobals.ActiveVessel.situation == Vessel.Situations.LANDED ||
							    FlightGlobals.ActiveVessel.situation == Vessel.Situations.SPLASHED) && landingPermission && station == plan.destination) {
								plan.type = FlightPlanType.None;
							}
							if (GUILayout.Button("Cancel flight plan")) {
								postMessage(section.name+", this is "+Callsign+", we’d like to cancel our flight plan.",true);
								plan.type = FlightPlanType.None;
								startTimeout("CFP", 300);
							}
						}
						if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.LANDED ||
						    FlightGlobals.ActiveVessel.situation == Vessel.Situations.SPLASHED ||
						    FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH ) {
							if (station.ground != null) {
								if (landingPermission) {
									flightPermission = false;
									landingPermission = false;
								}
								if (GUILayout.Button("Tune "+station.ground.name+" on "+station.ground.frequency.ToString())) {
									section = station.ground;
								}
							}
						}
				    } else if (section.type==ATCclass.Ground) {
						if (!planning) {
							if (station.tower != null && ((plan.type != FlightPlanType.Orbital && plan.type != FlightPlanType.Suborbital) || FlightGlobals.ActiveVessel.situation != Vessel.Situations.PRELAUNCH)) {
								if (GUILayout.Button("Tune "+station.tower.name+" on "+station.tower.frequency.ToString())) {
									section = station.tower;
									stationContacted = true;
									if (FlightGlobals.ActiveVessel.situation==Vessel.Situations.LANDED || FlightGlobals.ActiveVessel.situation==Vessel.Situations.SPLASHED) {
										if (FlightGlobals.ActiveVessel.srfSpeed<5) {
											flightPermission = false;
										}
									} else if (FlightGlobals.ActiveVessel.situation==Vessel.Situations.FLYING && FlightGlobals.ActiveVessel.srfSpeed > 10 && flightPermission==false) {
										postMessage(Callsign+", you were not cleared to take off.", false);
										if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER) {
											Reputation.Instance.AddReputation(-50,TransactionReasons.Any);
										}
										flightPermission = true;
									}
								}
							}
							if (station.space_center != null && plan.type != FlightPlanType.HighAltitude && plan.type != FlightPlanType.LowAltitude) {
								if (GUILayout.Button("Tune "+station.space_center.name+" on "+station.space_center.frequency.ToString())) {
									section = station.space_center;
									stationContacted = true;
								}
							}
						}
						if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.LANDED || FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH || FlightGlobals.ActiveVessel.situation == Vessel.Situations.SPLASHED) {
							if (plan.type == FlightPlanType.None || planning) {
								doFlightPlanGUI();
							} else {
								if (plan.type == FlightPlanType.LowAltitude || plan.type == FlightPlanType.HighAltitude) {
									if (GUILayout.Button("Request cruising altitude increase")) {
										postMessage(section.name+", "+Callsign+" would like to increase cruising altitude.", true);
										startTimeout("RCI",200);
										plan.altitude += 1000;
									}
									if (GUILayout.Button("Request cruising altitude decrease")) {
										postMessage(section.name+", "+Callsign+" would like to decrease cruising altitude.", true);
										startTimeout("RCD",200);
										plan.altitude -= 1000;
									}
								}
								if (GUILayout.Button("Cancel flight plan")) {
									postMessage(section.name+", this is "+Callsign+", we’d like to cancel our flight plan.",true);
									plan.type = FlightPlanType.None;
									startTimeout("CFP", 300);
								}
							}
							if (!planning) {
								if (GUILayout.Button("Request taxi to parking")) {
									postMessage(section.name+", "+Callsign+". Request taxi to parking.", true);
									startTimeout("TTP", 300);
								}
								if (!station.runway.isOnRunway(FlightGlobals.ActiveVessel.latitude, FlightGlobals.ActiveVessel.longitude)) {
									if (GUILayout.Button("Request taxi to runway")) {
										
									}
								}
							}
						}
					} else if (section.type==ATCclass.Approach) {
						if (!stationContacted) {
							if (GUILayout.Button("Contact "+section.name)) {
								string altTarget = (plan.type==FlightPlanType.LowAltitude || plan.type == FlightPlanType.HighAltitude || (plan.type==FlightPlanType.Suborbital&&hasBeenToSpace)) ? " for "+Math.Min(plan.altitude,glideAltitude).ToString() : "";
								if (FlightGlobals.ActiveVessel.verticalSpeed>50) {
									postMessage(section.name+", this is "+Callsign+", ascending through "+(1000*(int)((FlightGlobals.ActiveVessel.altitude+500)/1000)).ToString()+altTarget+".",true);
								} else if (FlightGlobals.ActiveVessel.verticalSpeed<-50) {
									postMessage(section.name+", this is "+Callsign+", descending through "+(1000*(int)((FlightGlobals.ActiveVessel.altitude+500)/1000)).ToString()+altTarget+".",true);
								} else {
									postMessage(section.name+", this is "+Callsign+" with you, "+(1000*(int)((FlightGlobals.ActiveVessel.altitude+500)/1000)).ToString()+".",true);
								}
								startTimeout("CON", 500);
								stationContacted = true;
							}
						} else { 
							if (station.distance()>30000 || FlightGlobals.ActiveVessel.altitude>15000) {
								if (!transferring) {
									postMessage(Callsign+", contact "+central.name+" on "+central.frequency+".", false);
									startTimeout("NUL", 200);
									transferring = true;
								} else {
									if (GUILayout.Button("Tune "+central.name+" on "+central.frequency.ToString())) {
										postMessage("Going to "+central.frequency+", "+Callsign+".", true);
										station = centralStation;
										section = central;
										transferring = false;
										stationContacted = false;
									}
								}
							}
							if (plan.type != FlightPlanType.None && plan.type != FlightPlanType.Orbital && !planning) {
								if (plan.destination.distance()<10000 && plan.destination.tower != null) {
									if (!transferring) {
										postMessage(Callsign+", contact "+plan.destination.tower.name+" on "+plan.destination.tower.frequency+".", false);
										startTimeout("NUL", 200);
										transferring = true;
									} else {
										if (GUILayout.Button("Tune "+plan.destination.tower.name+" on "+plan.destination.tower.frequency.ToString())) {
											postMessage("Going to "+plan.destination.tower.frequency+", "+Callsign+".", true);
											station = plan.destination;
											section = plan.destination.tower;
											transferring = false;
											stationContacted = false;
										}
									}
								} else {
									if (plan.type == FlightPlanType.LowAltitude || plan.type == FlightPlanType.HighAltitude) {
										if (GUILayout.Button("Request cruising altitude increase")) {
											postMessage(section.name+", "+Callsign+" would like to increase cruising altitude.", true);
											startTimeout("NUL",200);
											plan.altitude += 1000;
											timer = 1;
										}
										if (GUILayout.Button("Request cruising altitude decrease")) {
											postMessage(section.name+", "+Callsign+" would like to decrease cruising altitude.", true);
											startTimeout("NUL",200);
											plan.altitude -= 1000;
											timer = 1;
										}
									}
									if (timer==0) {
										if (plan.type == FlightPlanType.LowAltitude || plan.type == FlightPlanType.HighAltitude || (plan.type == FlightPlanType.Suborbital && hasBeenToSpace)) {
											if (Math.Abs(RelativeHeading())>5) {
												if (Math.Min(plan.altitude,glideAltitude) - FlightGlobals.ActiveVessel.altitude > 300) {
													postMessage(Callsign+", turn "+(RelativeHeading()>0?"right":"left")+" heading "+plan.destination.heading().ToString()+", climb and maintain "+Math.Min(plan.altitude,glideAltitude)+".", false);
												} else if (Math.Min(plan.altitude,glideAltitude) - FlightGlobals.ActiveVessel.altitude < -300) {
													postMessage(Callsign+", turn "+(RelativeHeading()>0?"right":"left")+" heading "+plan.destination.heading().ToString()+", descend and maintain "+Math.Min(plan.altitude,glideAltitude)+".", false);
												} else {
													postMessage(Callsign+", turn "+(RelativeHeading()>0?"right":"left")+" heading "+plan.destination.heading().ToString()+".",false);
												}
											} else {
												if (Math.Min(plan.altitude,glideAltitude) - FlightGlobals.ActiveVessel.altitude > 300) {
													postMessage(Callsign+", climb and maintain "+Math.Min(plan.altitude,glideAltitude)+".", false);
												} else if (Math.Min(plan.altitude,glideAltitude) - FlightGlobals.ActiveVessel.altitude < -300) {
													postMessage(Callsign+", descend and maintain "+Math.Min(plan.altitude,glideAltitude)+".", false);
												} else {
												}
											}
										}
										timer = 3000;
									} else {
										timer -= 1;
									}
								}
							} else if (station.distance()<10000 && station.tower != null) {
								if (!transferring) {
									postMessage(Callsign+", contact "+station.tower.name+" on "+station.tower.frequency+".", false);
									startTimeout("NUL", 200);
									transferring = true;
								} else {
									if (GUILayout.Button("Tune "+station.tower.name+" on "+station.tower.frequency.ToString())) {
										postMessage("Going to "+station.tower.frequency+", "+Callsign+".", true);
										section = station.tower;
										transferring = false;
										stationContacted = false;
									}
								}
							}
							if (plan.type != FlightPlanType.None && !planning) {
								if (GUILayout.Button("Cancel flight plan")) {
									postMessage(section.name+", this is "+Callsign+", we’d like to cancel our flight plan.",true);
									plan.type = FlightPlanType.None;
									startTimeout("CFP", 300);
								}
							} else {
								doFlightPlanGUI(true);
							}
						}
					} else if (section.type == ATCclass.Central) {
						if (FlightGlobals.ActiveVessel.altitude>50000) {
							if (!transferring) {
								postMessage(Callsign+", contact "+station.space_center.name+" on "+station.space_center.frequency+".", false);
								startTimeout("NUL", 200);
								transferring = true;
							} else {
								if (GUILayout.Button("Tune "+station.space_center.name+" on "+station.space_center.frequency.ToString())) {
									postMessage("Going to "+station.space_center.frequency+", "+Callsign+".", true);
									section = station.space_center;
									transferring = false;
									stationContacted = false;
								}
							}
						} else {
							if (!stationContacted) {
								if (GUILayout.Button("Contact "+section.name)) {
									string altTarget = (plan.type==FlightPlanType.LowAltitude || plan.type == FlightPlanType.HighAltitude || (plan.type==FlightPlanType.Suborbital&&hasBeenToSpace)) ? " for "+Math.Min(plan.altitude,glideAltitude).ToString() : "";
									if (FlightGlobals.ActiveVessel.verticalSpeed>50) {
										postMessage(section.name+", this is "+Callsign+", ascending through "+(1000*(int)((FlightGlobals.ActiveVessel.altitude+500)/1000)).ToString()+altTarget+".",true);
									} else if (FlightGlobals.ActiveVessel.verticalSpeed<-50) {
										postMessage(section.name+", this is "+Callsign+", descending through "+(1000*(int)((FlightGlobals.ActiveVessel.altitude+500)/1000)).ToString()+altTarget+".",true);
									} else {
										postMessage(section.name+", this is "+Callsign+" with you, "+(1000*(int)((FlightGlobals.ActiveVessel.altitude+500)/1000)).ToString()+".",true);
									}
									startTimeout("CON", 500);
									stationContacted = true;
								}
							} else {
								if (approachStation.distance()<30000 && FlightGlobals.ActiveVessel.altitude<15000) {
									if (!transferring) {
										postMessage(Callsign+", contact "+approach.name+" on "+approach.frequency+".", false);
										startTimeout("NUL", 200);
										transferring = true;
									} else {
										if (GUILayout.Button("Tune "+approach.name+" on "+approach.frequency.ToString())) {
											postMessage("Going to "+approach.frequency+", "+Callsign+".", true);
											station = approachStation;
											section = approach;
											transferring = false;
											stationContacted = false;
										}
									}
								} else if (towerStation.distance()<10000) {
									if (!transferring) {
										postMessage(Callsign+", contact "+tower.name+" on "+tower.frequency+".", false);
										startTimeout("NUL", 200);
										transferring = true;
									} else {
										if (GUILayout.Button("Tune "+tower.name+" on "+tower.frequency.ToString())) {
											postMessage("Going to "+tower.frequency+", "+Callsign+".", true);
											station = towerStation;
											section = tower;
											transferring = false;
											stationContacted = false;
										}
									}
								} else {
									if (centralStation != station) {
										if (!transferring) {
											postMessage(Callsign+", contact "+central.name+" on "+central.frequency+".", false);
											startTimeout("NUL", 200);
											transferring = true;
										} else {
											if (GUILayout.Button("Tune "+central.name+" on "+central.frequency.ToString())) {
												postMessage("Going to "+central.frequency+", "+Callsign+".", true);
												station = centralStation;
												section = central;
												transferring = false;
												stationContacted = false;
											}
										}
									} else {
										if (plan.type != FlightPlanType.None && !planning) {
											if (timer==0) {
												if (plan.type == FlightPlanType.LowAltitude || plan.type == FlightPlanType.HighAltitude || (plan.type == FlightPlanType.Suborbital && hasBeenToSpace)) {
													if (Math.Abs(RelativeHeading())>5) {
														if (Math.Min(plan.altitude,glideAltitude) - FlightGlobals.ActiveVessel.altitude > 300) {
															postMessage(Callsign+", turn "+(RelativeHeading()>0?"right":"left")+" heading "+plan.destination.heading().ToString()+", climb and maintain "+Math.Min(plan.altitude,glideAltitude)+".", false);
														} else if (Math.Min(plan.altitude,glideAltitude) - FlightGlobals.ActiveVessel.altitude < -300) {
															postMessage(Callsign+", turn "+(RelativeHeading()>0?"right":"left")+" heading "+plan.destination.heading().ToString()+", descend and maintain "+Math.Min(plan.altitude,glideAltitude)+".", false);
														} else {
															postMessage(Callsign+", turn "+(RelativeHeading()>0?"right":"left")+" heading "+plan.destination.heading().ToString()+".",false);
														}
													} else {
														if (Math.Min(plan.altitude,glideAltitude) - FlightGlobals.ActiveVessel.altitude > 300) {
															postMessage(Callsign+", climb and maintain "+Math.Min(plan.altitude,glideAltitude)+".", false);
														} else if (Math.Min(plan.altitude,glideAltitude) - FlightGlobals.ActiveVessel.altitude < -300) {
															postMessage(Callsign+", descend and maintain "+Math.Min(plan.altitude,glideAltitude)+".", false);
														} else {
														}
													}
												}
												timer = 3000;
											} else {
												timer -= 1;
											}
											if (GUILayout.Button("Cancel flight plan")) {
												postMessage(section.name+", this is "+Callsign+", we’d like to cancel our flight plan.",true);
												plan.type = FlightPlanType.None;
												startTimeout("CFP", 300);
											}
										} else {
											doFlightPlanGUI(true);
										}
									}
								}
							}
						}
					} else if (section.type == ATCclass.Space_Center) {
						if (!stationContacted) {
							if (GUILayout.Button("Contact "+section.name)) {
								if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.FLYING) {
									if (FlightGlobals.ActiveVessel.verticalSpeed>50) {
										postMessage(section.name+", this is "+Callsign+", ascending through "+(1000*(int)((FlightGlobals.ActiveVessel.altitude+500)/1000)).ToString()+".",true);
									} else if (FlightGlobals.ActiveVessel.verticalSpeed<-50) {
										postMessage(section.name+", this is "+Callsign+", descending through "+(1000*(int)((FlightGlobals.ActiveVessel.altitude+500)/1000)).ToString()+".",true);
									} else {
										postMessage(section.name+", this is "+Callsign+" with you, "+(1000*(int)((FlightGlobals.ActiveVessel.altitude+500)/1000)).ToString()+".",true);
									}
								} else {
									postMessage (section.name+", this is "+Callsign+".", true);
								}
								startTimeout("CON", 500);
								stationContacted = true;
							}
						} else {
							if (spaceCenterStation != station) {
								if (!transferring) {
									postMessage(Callsign+", contact "+spaceCenter.name+" on "+spaceCenter.frequency+".", false);
									startTimeout("NUL", 200);
									transferring = true;
								} else {
									if (GUILayout.Button("Tune "+spaceCenter.name+" on "+spaceCenter.frequency.ToString())) {
										postMessage("Going to "+spaceCenter.frequency+", "+Callsign+".", true);
										station = spaceCenterStation;
										section = spaceCenter;
										transferring = false;
										stationContacted = false;
									}
								}
							} else {
								if (hasBeenToSpace) {
									flightPermission = true;
									if (FlightGlobals.ActiveVessel.altitude < 50000) {
										if (!transferring) {
											postMessage(Callsign+", contact "+central.name+" on "+central.frequency+".", false);
											startTimeout("NUL", 200);
											transferring = true;
										} else {
											if (GUILayout.Button("Tune "+central.name+" on "+central.frequency.ToString())) {
												postMessage("Going to "+central.frequency+", "+Callsign+".", true);
												station = centralStation;
												section = central;
												transferring = false;
												stationContacted = false;
											}
										}
									} else if (FlightGlobals.ActiveVessel.altitude > 69200) {
										if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.ORBITING && plan.type == FlightPlanType.Orbital) {
											plan.type = FlightPlanType.None;
										}
									}
								}
							}
							if (!flightPermission) {
								if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH) {
									if (GUILayout.Button("Request launch clearance")) {
										postMessage(section.name + ", " + Callsign + " here, request clearance for launch.", true);
										flightPermission = true;
										landingPermission = false;
										timer = 700;
										startTimeout("RLU", 250);
									}
								} else if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.FLYING) {
									postMessage(Callsign+", you were not cleared to take off.", false);
									if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER) {
										Reputation.Instance.AddReputation(-50,TransactionReasons.Any);
									}
									flightPermission = true;
								} else if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.LANDED) {
									if (GUILayout.Button("Request takeoff clearance")) {
										contactStation();
										flightPermission = true;
										landingPermission = false;
										timer = 700;
										startTimeout("RTC", 250);
									}
								}
							}
							if (station.ground==null && plan.type == FlightPlanType.None && !planning) {
								doFlightPlanGUI();
							}
						}
					}
			    }
//				response = GUILayout.Button(response, "Something"); 
				GUILayout.EndVertical();
				GUI.DragWindow();
			}
		}

		void startTimeout(string action, int ticks) {
			actionToRun = action;
			ticksRemaining = ticks;
		}

		void postMessage(string message, bool me) {
			message4 = message3;
			message3 = message2;
			message2 = message1;
			message1 = message;
			who4 = who3;
			who3 = who2;
			who2 = who1;
			who1 = me;
		}

		void contactStation() {
			postMessage(section.name + ", " + Callsign + " is at " + station.runway.name + ", request clearance for takeoff.", true);
		}

		void runAction(string action) {
			if (section.type == ATCclass.Ground) {
				if (action == "TTP") {
					postMessage (Callsign + ", taxi to parking.", false);
				} else if (action == "SFP") {
					if (plan.type == FlightPlanType.LowAltitude || plan.type == FlightPlanType.HighAltitude) {
						postMessage (Callsign + ", you have been filed for a flight to " + plan.destination.name + ", cruising altitude " + plan.altitude.ToString () + " meters.", false);
					} else if (plan.type == FlightPlanType.Suborbital) {
						postMessage (Callsign + ", you have been filed for an exo-atmospheric flight to " + plan.destination.name + ".", false);
					} else if (plan.type == FlightPlanType.Orbital) {
						postMessage (Callsign + ", you have been filed for an ascent to orbit.", false);
					}
				} else if (action == "RCI") {
					postMessage (Callsign + ", cruising altitude increased to " + plan.altitude, false);
				} else if (action == "RCD") {
					postMessage (Callsign + ", cruising altitude decreased to " + plan.altitude, false);
				} else if (action == "CFP") {
					postMessage ("Ok, " + Callsign + ", we’ve cancelled your flight plan.", false);
				}
			} else if (section.type == ATCclass.Tower) {
				if (action == "RTC") {
					// if (station.runway.isOnRunway()) {
					if (plan.type == FlightPlanType.None) {
						postMessage (Callsign + ", cleared for takeoff.", false);
					} else if (plan.type == FlightPlanType.LowAltitude || plan.type == FlightPlanType.HighAltitude) {
						postMessage (Callsign + ", cleared for takeoff. Fly runway heading, climb and maintain " + Math.Min(plan.altitude,glideAltitude).ToString () + " meters.", false);
					} else if (plan.type == FlightPlanType.Suborbital) {
						postMessage (Callsign + ", cleared for takeoff and suborbital hop.", false);
					} else if (plan.type == FlightPlanType.Orbital) {
						postMessage (Callsign + ", cleared for takeoff and ascent to orbit.", false);
					}
					// }
				} else if (action == "CFP") {
					postMessage ("Ok, " + Callsign + ", we’ve cancelled your flight plan.", false);
				} else if (action == "RLC") {
					postMessage (Callsign + ", cleared for landing, " + ((station.runway.altname!="" && (station.heading()-station.runway.heading+630)%360<180)?station.runway.altname:station.runway.name) + ".", false);
				} else if (action == "RTG") {
					postMessage (Callsign + ", cleared for touch-and-go, " + ((station.runway.altname!="" && (station.heading()-station.runway.heading+630)%360<180)?station.runway.altname:station.runway.name) + ".", false);
				} else if (action == "AGA") {
					postMessage (Callsign + ", roger.", false);
				} else if (action == "CLI") {
					postMessage (Callsign + ", roger.", false);
				} else if (action == "CON") {
					if (plan.type == FlightPlanType.None || plan.type == FlightPlanType.Orbital) {
						postMessage (Callsign + ", continue on course.", false);
					} else {
						postMessage (Callsign + ", roger. Head straight in, enter pattern for " + ((station.runway.altname!="" && (station.heading()-station.runway.heading+630)%360<180)?station.runway.altname:station.runway.name) + ".", false);
					}
				}
			} else if (section.type == ATCclass.Approach) {
				if (action == "CON") {
					postMessage (Callsign + ", continue on course.", false);
				} else if (action == "CFP") {
					postMessage ("Ok, " + Callsign + ", we’ve cancelled your flight plan.", false);
				} else if (action == "SFP") {
					if (plan.type == FlightPlanType.LowAltitude || plan.type == FlightPlanType.HighAltitude) {
						postMessage (Callsign + ", you have been filed for a flight to " + plan.destination.name + ", cruising altitude " + plan.altitude.ToString () + " meters.", false);
					} else if (plan.type == FlightPlanType.Suborbital) {
						postMessage (Callsign + ", you have been filed for an exo-atmospheric flight to " + plan.destination.name + ".", false);
					} else if (plan.type == FlightPlanType.Orbital) {
						postMessage (Callsign + ", you have been filed for an ascent to orbit.", false);
					}
				}
			} else if (section.type == ATCclass.Central) {
				if (action == "CON") {
					postMessage (Callsign + ", continue on course.", false);
				} else if (action == "CFP") {
					postMessage ("Ok, " + Callsign + ", we’ve cancelled your flight plan.", false);
				} else if (action == "SFP") {
					if (plan.type == FlightPlanType.LowAltitude || plan.type == FlightPlanType.HighAltitude) {
						postMessage (Callsign + ", you have been filed for a flight to " + plan.destination.name + ", cruising altitude " + plan.altitude.ToString () + " meters.", false);
					} else if (plan.type == FlightPlanType.Suborbital) {
						postMessage (Callsign + ", you have been filed for an exo-atmospheric flight to " + plan.destination.name + ".", false);
					} else if (plan.type == FlightPlanType.Orbital) {
						postMessage (Callsign + ", you have been filed for an ascent to orbit.", false);
					}
				}
			} else if (section.type == ATCclass.Space_Center) {
				if (action == "RTC") {
					// if (station.runway.isOnRunway()) {
					if (plan.type == FlightPlanType.None) {
						postMessage (Callsign + ", cleared for takeoff.", false);
					} else if (plan.type == FlightPlanType.LowAltitude || plan.type == FlightPlanType.HighAltitude) {
						postMessage (Callsign + ", cleared for takeoff. Fly runway heading, climb and maintain " + Math.Min(plan.altitude,glideAltitude).ToString () + " meters.", false);
					} else if (plan.type == FlightPlanType.Suborbital) {
						postMessage (Callsign + ", cleared for takeoff and suborbital hop.", false);
					} else if (plan.type == FlightPlanType.Orbital) {
						postMessage (Callsign + ", cleared for takeoff and ascent to orbit.", false);
					}
					// }
				} else if (action == "RLU") {
					postMessage (Callsign + ", cleared for launch.", false);
				} else if (action == "CON") {
					postMessage (Callsign + ", " + section.name + ". Read you loud and clear.", false);
				} else if (action == "CFP") {
					postMessage ("Ok, " + Callsign + ", we’ve cancelled your flight plan.", false);
				} else if (action == "SFP") {
					if (plan.type == FlightPlanType.LowAltitude || plan.type == FlightPlanType.HighAltitude) {
						postMessage (Callsign + ", you have been filed for a flight to " + plan.destination.name + ", cruising altitude " + plan.altitude.ToString () + " meters.", false);
					} else if (plan.type == FlightPlanType.Suborbital) {
						postMessage (Callsign + ", you have been filed for an exo-atmospheric flight to " + plan.destination.name + ".", false);
					} else if (plan.type == FlightPlanType.Orbital) {
						postMessage (Callsign + ", you have been filed for an ascent to orbit.", false);
					}
				}
			}
		}
	}
}

