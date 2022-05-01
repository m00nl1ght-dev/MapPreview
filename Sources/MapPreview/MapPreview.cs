/*
 
Modified version of: https://github.com/UnlimitedHugs/RimworldMapReroll/blob/master/Source/UI/Widget_MapPreview.cs

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
using MapReroll.Interpolation;
using MapReroll.Promises;
using MapReroll.UI;
using UnityEngine;
using Verse;

namespace MapPreview;

public class MapPreview : IDisposable, IEquatable<MapPreview> {
	
	private const float SpawnInterpolationDuration = .3f;

	private static readonly Color OutlineColor = GenColor.FromHex("616C7A");

	private readonly string _seed;
	private readonly IPromise<Texture2D> _promise;
		
	private ValueInterpolator _spawnInterpolator;
	private ValueInterpolator _zoomInterpolator;
	private Texture2D _previewTex;

	public MapPreview(IPromise<Texture2D> promise, string seed) {
		PrepareComponents();
		_promise = promise;
		promise.Done(OnPromiseResolved);
		_seed = seed;
	}

	public MapPreview(MapPreview copyFrom) {
		PrepareComponents();
		_promise = copyFrom._promise;
		_promise.Done(OnPromiseResolved);
		_seed = copyFrom._seed;
	}

	private void PrepareComponents() {
		_spawnInterpolator = new ValueInterpolator();
		_zoomInterpolator = new ValueInterpolator();
	}

	public void Dispose() {
		UnityEngine.Object.Destroy(_previewTex);
		_previewTex = null;
	}

	public void Draw(Rect inRect, int index, bool interactive) {
		if (Event.current.type == EventType.Repaint) {
			_spawnInterpolator.Update();
			_zoomInterpolator.Update();
			if (_spawnInterpolator.value < 1) {
				Widget_RerollPreloader.Draw(inRect.center, index);
			}
		}
		DrawOutline(inRect);
		if (_previewTex != null) {
			var texScale = _spawnInterpolator.value;
			var texRect = inRect.ScaledBy(texScale).ContractedBy(1f);
			GUI.DrawTexture(texRect, _previewTex);
		}
	}

	private void OnPromiseResolved(Texture2D tex) {
		_previewTex = tex;
		_spawnInterpolator.value = 0f;
		_spawnInterpolator.StartInterpolation(1f, SpawnInterpolationDuration, CurveType.CubicOut);
	}

	private void DrawOutline(Rect rect)
	{
		var oldColor = GUI.color;
		GUI.color = OutlineColor;
		Widgets.DrawBox(rect);
		GUI.color = oldColor;
	}

	public bool Equals(MapPreview other) {
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return string.Equals(_seed, other._seed);
	}

	public override bool Equals(object obj) {
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != GetType()) return false;
		return Equals((MapPreview)obj);
	}

	public override int GetHashCode() {
		return (_seed != null ? _seed.GetHashCode() : 0);
	}
}