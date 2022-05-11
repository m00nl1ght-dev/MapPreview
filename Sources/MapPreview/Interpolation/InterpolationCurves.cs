/*
 
Modified version of: https://github.com/UnlimitedHugs/RimworldMapReroll/blob/master/Source/Interpolation/InterpolationCurves.cs

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

using System.Collections.Generic;
using UnityEngine;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace MapPreview.Interpolation
{
    public enum CurveType
    {
        Linear,
        QuadraticIn,
        QuadraticOut,
        QuadraticInOut,
        CubicIn,
        CubicOut,
        CubicInOut,
        QuarticIn,
        QuarticOut,
        QuarticInOut,
        QuinticIn,
        QuinticOut,
        QuinticInOut,
        SinusoidalIn,
        SinusoidalOut,
        SinusoidalInOut,
        ExponentialIn,
        ExponentialOut,
        ExponentialInOut,
        CircularIn,
        CircularOut,
        CircularInOut,
        ElasticIn,
        ElasticOut,
        ElasticInOut,
        BackIn,
        BackOut,
        BackInOut,
        BounceIn,
        BounceOut,
        BounceInOut
    }

    /**
	 * These are functions that describe the change of a value over time. See http://easings.net/ for more info.
	 * Swiped from https://gist.github.com/Fonserbc/3d31a25e87fdaa541ddf
	 */
    public static class InterpolationCurves
    {
        public delegate float Curve(float time);

        public static readonly Dictionary<CurveType, Curve> AllCurves = new Dictionary<CurveType, Curve>
        {
            { CurveType.Linear, Linear },
            { CurveType.QuadraticIn, Quadratic.In }, { CurveType.QuadraticOut, Quadratic.Out },
            { CurveType.QuadraticInOut, Quadratic.InOut },
            { CurveType.CubicIn, Cubic.In }, { CurveType.CubicOut, Cubic.Out }, { CurveType.CubicInOut, Cubic.InOut },
            { CurveType.QuarticIn, Quartic.In }, { CurveType.QuarticOut, Quartic.Out },
            { CurveType.QuarticInOut, Quartic.InOut },
            { CurveType.QuinticIn, Quintic.In }, { CurveType.QuinticOut, Quintic.Out },
            { CurveType.QuinticInOut, Quintic.InOut },
            { CurveType.SinusoidalIn, Sinusoidal.In }, { CurveType.SinusoidalOut, Sinusoidal.Out },
            { CurveType.SinusoidalInOut, Sinusoidal.InOut },
            { CurveType.ExponentialIn, Exponential.In }, { CurveType.ExponentialOut, Exponential.Out },
            { CurveType.ExponentialInOut, Exponential.InOut },
            { CurveType.CircularIn, Circular.In }, { CurveType.CircularOut, Circular.Out },
            { CurveType.CircularInOut, Circular.InOut },
            { CurveType.ElasticIn, Elastic.In }, { CurveType.ElasticOut, Elastic.Out },
            { CurveType.ElasticInOut, Elastic.InOut },
            { CurveType.BackIn, Back.In }, { CurveType.BackOut, Back.Out }, { CurveType.BackInOut, Back.InOut },
            { CurveType.BounceIn, Bounce.In }, { CurveType.BounceOut, Bounce.Out },
            { CurveType.BounceInOut, Bounce.InOut }
        };

        public static float Linear(float t)
        {
            return t;
        }

        public class Quadratic
        {
            public static float In(float t)
            {
                return t * t;
            }

            public static float Out(float t)
            {
                return t * (2f - t);
            }

            public static float InOut(float t)
            {
                if ((t *= 2f) < 1f) return 0.5f * t * t;
                return -0.5f * ((t -= 1f) * (t - 2f) - 1f);
            }
        };

        public class Cubic
        {
            public static float In(float t)
            {
                return t * t * t;
            }

            public static float Out(float t)
            {
                return 1f + ((t -= 1f) * t * t);
            }

            public static float InOut(float t)
            {
                if ((t *= 2f) < 1f) return 0.5f * t * t * t;
                return 0.5f * ((t -= 2f) * t * t + 2f);
            }
        };

        public class Quartic
        {
            public static float In(float t)
            {
                return t * t * t * t;
            }

            public static float Out(float t)
            {
                return 1f - ((t -= 1f) * t * t * t);
            }

            public static float InOut(float t)
            {
                if ((t *= 2f) < 1f) return 0.5f * t * t * t * t;
                return -0.5f * ((t -= 2f) * t * t * t - 2f);
            }
        };

        public class Quintic
        {
            public static float In(float t)
            {
                return t * t * t * t * t;
            }

            public static float Out(float t)
            {
                return 1f + ((t -= 1f) * t * t * t * t);
            }

            public static float InOut(float t)
            {
                if ((t *= 2f) < 1f) return 0.5f * t * t * t * t * t;
                return 0.5f * ((t -= 2f) * t * t * t * t + 2f);
            }
        };

        public class Sinusoidal
        {
            public static float In(float t)
            {
                return 1f - Mathf.Cos(t * Mathf.PI / 2f);
            }

            public static float Out(float t)
            {
                return Mathf.Sin(t * Mathf.PI / 2f);
            }

            public static float InOut(float t)
            {
                return 0.5f * (1f - Mathf.Cos(Mathf.PI * t));
            }
        };

        public class Exponential
        {
            public static float In(float t)
            {
                return t == 0f ? 0f : Mathf.Pow(1024f, t - 1f);
            }

            public static float Out(float t)
            {
                return t == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
            }

            public static float InOut(float t)
            {
                if (t == 0f) return 0f;
                if (t == 1f) return 1f;
                if ((t *= 2f) < 1f) return 0.5f * Mathf.Pow(1024f, t - 1f);
                return 0.5f * (-Mathf.Pow(2f, -10f * (t - 1f)) + 2f);
            }
        };

        public class Circular
        {
            public static float In(float t)
            {
                return 1f - Mathf.Sqrt(1f - t * t);
            }

            public static float Out(float t)
            {
                return Mathf.Sqrt(1f - ((t -= 1f) * t));
            }

            public static float InOut(float t)
            {
                if ((t *= 2f) < 1f) return -0.5f * (Mathf.Sqrt(1f - t * t) - 1);
                return 0.5f * (Mathf.Sqrt(1f - (t -= 2f) * t) + 1f);
            }
        };

        public class Elastic
        {
            public static float In(float t)
            {
                if (t == 0) return 0;
                if (t == 1) return 1;
                return -Mathf.Pow(2f, 10f * (t -= 1f)) * Mathf.Sin((t - 0.1f) * (2f * Mathf.PI) / 0.4f);
            }

            public static float Out(float t)
            {
                if (t == 0) return 0;
                if (t == 1) return 1;
                return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - 0.1f) * (2f * Mathf.PI) / 0.4f) + 1f;
            }

            public static float InOut(float t)
            {
                if ((t *= 2f) < 1f)
                    return -0.5f * Mathf.Pow(2f, 10f * (t -= 1f)) * Mathf.Sin((t - 0.1f) * (2f * Mathf.PI) / 0.4f);
                return Mathf.Pow(2f, -10f * (t -= 1f)) * Mathf.Sin((t - 0.1f) * (2f * Mathf.PI) / 0.4f) * 0.5f + 1f;
            }
        };

        public class Back
        {
            static float s = 1.70158f;
            static float s2 = 2.5949095f;

            public static float In(float t)
            {
                return t * t * ((s + 1f) * t - s);
            }

            public static float Out(float t)
            {
                return (t -= 1f) * t * ((s + 1f) * t + s) + 1f;
            }

            public static float InOut(float t)
            {
                if ((t *= 2f) < 1f) return 0.5f * (t * t * ((s2 + 1f) * t - s2));
                return 0.5f * ((t -= 2f) * t * ((s2 + 1f) * t + s2) + 2f);
            }
        };

        public class Bounce
        {
            public static float In(float t)
            {
                return 1f - Out(1f - t);
            }

            public static float Out(float t)
            {
                if (t < (1f / 2.75f))
                {
                    return 7.5625f * t * t;
                }
                else if (t < (2f / 2.75f))
                {
                    return 7.5625f * (t -= (1.5f / 2.75f)) * t + 0.75f;
                }
                else if (t < (2.5f / 2.75f))
                {
                    return 7.5625f * (t -= (2.25f / 2.75f)) * t + 0.9375f;
                }
                else
                {
                    return 7.5625f * (t -= (2.625f / 2.75f)) * t + 0.984375f;
                }
            }

            public static float InOut(float t)
            {
                if (t < 0.5f) return In(t * 2f) * 0.5f;
                return Out(t * 2f - 1f) * 0.5f + 0.5f;
            }
        };
    }
}