﻿using UnityEngine;
using System.Collections;

public class StateGameMenu : GameState {
	
	public override void StateGUI() {
		GUILayout.Label ("state: MAIN MENU");
	}

	public override void StateUpdate() {
		print ("StateGameMenu::StateUpdate() ");
		Application.LoadLevel("MainMenu");
		
		GameManager.instance.NewGameState(GameManager.instance.stateGameMenu);
		
	}
}
