﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour {

	public bool isPortal = false;

	public bool isPellet;
	public bool isSuperPellet;
	public bool didConsume;

	public bool isGhostHouseEntrance;
	public bool isGhostHouse;

	public GameObject portalReceiver;
}
