﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

	public class DamageIndicator : FLabel
	{
        float xVel;
        float yVel;
        const float xMax = 200;
        const float yMax = 200;

        public DamageIndicator(Vector2 position) : base(C.SmallFont, "-1")
        {
            this.SetPosition(position);

            this.color = Color.red;
            xVel = RXRandom.Float() * xMax - xMax / 2;
            yVel = yMax;
        }

        public override void HandleAddedToStage()
        {
            Futile.instance.SignalUpdate += Update;
            base.HandleAddedToStage();
        }

        public override void HandleRemovedFromStage()
        {
            Futile.instance.SignalUpdate -= Update;
            base.HandleRemovedFromStage();
        }
        public void Update()
        {
            RXDebug.Log(this.GetPosition());
            this.x += xVel * UnityEngine.Time.deltaTime;
            this.y += yVel * UnityEngine.Time.deltaTime;
            this.alpha -= 1.0f * UnityEngine.Time.deltaTime;
            this.scale -= 1.0f * UnityEngine.Time.deltaTime;
            yVel -= 800 * UnityEngine.Time.deltaTime;

            if (this.alpha <= 0)
                this.RemoveFromContainer();
        }
	}

