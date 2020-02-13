﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Letter : MonoBehaviour {
    private string letterString;
    public string LetterString {
        get => letterString;
        set => letterString = value;
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
            CollectibleEvents.InvokeLetterPickup(LetterString);
        }
    }
}
