﻿using Modding;
using On;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RandoMapMod {
	public class RandoMapMod : Mod {
		//TODO:
		//		Add option to the New Game setup screen to unlock all maps at start
		//			Or maybe add a YN shiny to the floor at Iselda's?
		//		Pretty up the sprites
		//		Add a pin-sprite for Sequence Breaking
		//		HOLD - I need RandomizerMod to be elsewise in a few places, in order to ultimately clean this all up
		//			LogicManager all public'd, and maybe change the parser to accept a callback function instead of the list of 'obtained' items.
		//			Either the SaveSettings needs to hang onto the StringValues a bit longer before removing the actions, or all the action types need to be public'd.
		//		HOLD - Give the randomizer a custom end screen (
		//			Skip credits, 
		//			add hash [of randomizer actions?],
		//			location check count percentage [as opposed to masks, and other stuff...]
		//			checks per hour
		//		)
		//		HOLD - Add pins to the Legend??
		const float MAP_MIN_X = -24.16f;
		const float MAP_MAX_X = 17.3f;
		const float MAP_MIN_Y = -12.58548f;
		const float MAP_MAX_Y = 15.6913f;

		private GameObject custPinGroup = null;
		private GameMap theMap;

		public static RandoMapMod Instance {
			get; private set;
		}

		public static bool IsRando {
			get {
				//return true;
				return RandomizerMod.RandomizerMod.Instance.Settings.Randomizer;
			}
		}

		public override string GetVersion() {
			string ver = "0.3.2";
			int minAPI = 45;

			bool apiTooLow = Convert.ToInt32( ModHooks.Instance.ModVersion.Split( '-' )[1] ) < minAPI;
			if ( apiTooLow ) {
				return ver + " (Update API)";
			}

			return ver;
		}

		public override void Initialize() {
			if ( Instance != null ) {
				DebugLog.Warn( "Initialized twice... Stop that." );
				return;
			}
			Instance = this;
			DebugLog.Log( "RandoMapMod Initializing..." );

			Resources.Initialize();
			
			On.GameMap.Start += this.GameMap_Start;							//Set up custom pins
			On.GameMap.WorldMap += this.GameMap_WorldMap;					//Set big map boundaries
			On.GameMap.SetupMapMarkers += this.GameMap_SetupMapMarkers;		//Enable the custom pins
			On.GameMap.DisableMarkers += this.GameMap_DisableMarkers;		//Disable the custom pins

			ModHooks.Instance.SavegameLoadHook += this.SavegameLoadHook;	//Load object name changes
			ModHooks.Instance.SavegameSaveHook += this.SavegameSaveHook;	//Load object name changes

			DebugLog.Log("RandoMapMod Initialize complete!");
		}

		private void SavegameLoadHook( int slot ) {
			ObjectNames.Load(slot);
		}

		private void SavegameSaveHook( int slot ) {
			ObjectNames.Load(slot);
		}

		private void GameMap_Start( On.GameMap.orig_Start orig, GameMap self ) {
			if ( !IsRando ) {
				orig( self );
				return;
			}

			//Create the custom pin group, and add all the new pins
			if ( this.custPinGroup == null ) {
				this.theMap = self;

				this.custPinGroup = new GameObject( "Custom Pins" );
				this.custPinGroup.AddComponent<PinGroup>();
				this.custPinGroup.transform.parent = self.transform;
				this.custPinGroup.transform.position = new Vector3( 0f, 0f, 0f );
				this.custPinGroup.SetActive( false );

				foreach ( PinData pin in PinData_S.All.Values ) {
					this.addPinToRoom( pin );
				}
			}

			orig( self );
		}

		private void GameMap_WorldMap( On.GameMap.orig_WorldMap orig, GameMap self ) {
			orig( self );
			if ( !IsRando )
				return;

			//Set the maximum scroll boundaries, so we can scroll the entire map, even if we don't have the maps unlocked.
			if ( self.panMinX > MAP_MIN_X )
				self.panMinX = MAP_MIN_X;
			if ( self.panMaxX < MAP_MAX_X )
				self.panMaxX = MAP_MAX_X;
			if ( self.panMinY > MAP_MIN_Y )
				self.panMinY = MAP_MIN_Y;
			if ( self.panMaxY < MAP_MAX_Y )
				self.panMaxY = MAP_MAX_Y;
		}

		private void GameMap_SetupMapMarkers( On.GameMap.orig_SetupMapMarkers orig, GameMap self ) {
			orig( self );
			if ( !IsRando )
				return;

			this.custPinGroup.SetActive( true );
		}

		private void GameMap_DisableMarkers( On.GameMap.orig_DisableMarkers orig, GameMap self ) {
			if ( !IsRando ) {
				orig( self );
				return;
			}
			this.custPinGroup.SetActive( false );

			orig( self );
		}

		private void addPinToRoom( PinData pin ) {
			string roomName = pin.PinScene;

			GameObject newPin = new GameObject( "pin_rando" );
			newPin.transform.parent = this.custPinGroup.transform;
			newPin.layer = 30;
			newPin.transform.localScale *= 1.2f;

			SpriteRenderer sr = newPin.AddComponent<SpriteRenderer>();
			sr.sprite = Resources.Sprite( "Map.randoPin" );
			sr.sortingLayerName = "HUD";
			sr.size = new Vector2( 1f, 1f );

			Pin pinC = newPin.AddComponent<Pin>();
			pinC.PinData = pin;

			Vector3 vec = this.getRoomPos( roomName ) + pin.Offset;
			newPin.transform.localPosition = new Vector3( vec.x, vec.y, (vec.z - 0.5f) );
		}

		private Vector3 getRoomPos( string prmRoomName ) {
			//TODO: I should probably just remove this stupid thing; it's a waste of cycles. All
			//		I'd have to do is print out the .localPosition of each Pin, then update the XML
			//		with THOSE offsets instead... On the other hand, this way simplifies things...
			Vector3 pos = new Vector3( -30f, -30f, -0.5f );
			bool exitLoop = false;

			for ( int index1 = 0; index1 < this.theMap.transform.childCount; ++index1 ) {
				GameObject gameObject1 = this.theMap.transform.GetChild( index1 ).gameObject;
				for ( int index2 = 0; index2 < gameObject1.transform.childCount; ++index2 ) {
					GameObject gameObject2 = gameObject1.transform.GetChild( index2 ).gameObject;
					if ( gameObject2.name == prmRoomName ) {
						pos = gameObject2.transform.position;
						exitLoop = true;
						break;
					}
				}
				if ( exitLoop ) {
					break;
				}
			}

			return pos;
		}
	}
}
