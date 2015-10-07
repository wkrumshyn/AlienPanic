﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SegmentManager {

	List<GameObject> list = null;

	const int MaxSegmentCount = 3;


	private SegmentManager() {
		list = new List<GameObject> ();

	}

	private static SegmentManager instance = null;
	private static SegmentManager get() {
		if (instance == null) {
			instance = new SegmentManager();
		}

		return instance;
	}

	public static void AddNewSegement(GameObject newSegment) {
		if (get ().list.Count >= MaxSegmentCount) {
			GameObject oldSegment = get ().list[0];
			get ().list.RemoveAt(0);
			GameObject.Destroy(oldSegment);
		}

		get ().list.Add (newSegment);
	}

	//TODO Make this
	public static string CreateRandomSegmentName() {
		return "Seg1";
	}

}