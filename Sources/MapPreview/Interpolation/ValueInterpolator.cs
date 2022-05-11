/*
 
Modified version of: https://github.com/UnlimitedHugs/RimworldMapReroll/blob/master/Source/Interpolation/ValueInterpolator.cs

MIT License

Copyright (c) 2017 UnlimitedHugs, modifications (c) 2022 m00nl1ght <https://github.com/m00nl1ght-dev>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

 */

using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace MapPreview.Interpolation
{
    /**
	 * Changes a float value over time according to an interpolation curve. Used for animation.
	 */
    public class ValueInterpolator : IExposable
    {
        public delegate void FinishedCallback(ValueInterpolator interpolator, float finalValue,
            float interpolationDuration, InterpolationCurves.Curve interpolationCurve);
        
        private static readonly BindingFlags AllBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public float value;
        public bool finished = true;
        public bool respectTimeScale;
        private float elapsedTime;
        private float initialValue;
        private float targetValue;
        private float duration;
        private string curveName;
        private InterpolationCurves.Curve curve;
        private FinishedCallback callback;

        // deserialization constructor
        public ValueInterpolator()
        {
        }

        public ValueInterpolator(float value = 0f)
        {
            this.value = value;
        }

        public ValueInterpolator StartInterpolation(float finalValue, float interpolationDuration, CurveType curveType)
        {
            initialValue = value;
            elapsedTime = 0;
            targetValue = finalValue;
            duration = interpolationDuration;
            curve = InterpolationCurves.AllCurves[curveType];
            finished = false;
            return this;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref value, "value");
            Scribe_Values.Look(ref finished, "finished", true);
            Scribe_Values.Look(ref respectTimeScale, "respectTimeScale", true);
            Scribe_Values.Look(ref elapsedTime, "elapsedTime");
            Scribe_Values.Look(ref initialValue, "initialValue");
            Scribe_Values.Look(ref targetValue, "targetValue");
            Scribe_Values.Look(ref duration, "duration");
            Scribe_Values.Look(ref duration, "duration");
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                curveName = curve?.Method.Name;
            }

            Scribe_Values.Look(ref curveName, "curveName");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (!curveName.NullOrEmpty())
                {
                    var curveMethod = typeof(InterpolationCurves).GetMethod(curveName, AllBindingFlags);
                    if (curveMethod == null)
                    {
                        Log.Error("Failed to load interpolation curve: " + curveName);
                    }
                    else
                    {
                        curve = (InterpolationCurves.Curve)Delegate.CreateDelegate(typeof(InterpolationCurves.Curve),
                            curveMethod, true);
                    }
                }
            }
        }

        public ValueInterpolator SetFinishedCallback(FinishedCallback finishedCallback)
        {
            callback = finishedCallback;
            return this;
        }

        public float UpdateIfUnpaused()
        {
            if (Find.TickManager.Paused) return value;
            return Update();
        }

        public float Update()
        {
            if (finished) return value;
            var deltaTime = Time.deltaTime;
            if (respectTimeScale) deltaTime *= Find.TickManager.TickRateMultiplier;
            elapsedTime += deltaTime;
            if (elapsedTime >= duration)
            {
                elapsedTime = duration;
                value = targetValue;
                finished = true;
                callback?.Invoke(this, value, duration, curve);
            }
            else
            {
                value = initialValue + curve(elapsedTime / duration) * (targetValue - initialValue);
            }

            return value;
        }

        public static implicit operator float(ValueInterpolator v)
        {
            return v.value;
        }
    }
}