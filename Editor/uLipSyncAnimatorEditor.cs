using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using System.Collections.Generic;

namespace uLipSync
{

	[CustomEditor(typeof(uLipSyncAnimator))]
	public class uLipSyncAnimatorEditor : Editor
	{
		uLipSyncAnimator uAnimator { get { return target as uLipSyncAnimator; } }
		ReorderableList _reorderableList = null;

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			if (EditorUtil.Foldout("LipSync Update Method", true))
			{
				++EditorGUI.indentLevel;
				EditorUtil.DrawProperty(serializedObject, nameof(uAnimator.updateMethod));
				--EditorGUI.indentLevel;
				EditorGUILayout.Separator();
			}

			if (EditorUtil.Foldout("Animator", true))
			{
				++EditorGUI.indentLevel;
				DrawAnimator();
				--EditorGUI.indentLevel;
				EditorGUILayout.Separator();
			}

			if (EditorUtil.Foldout("Parameters", true))
			{
				++EditorGUI.indentLevel;
				DrawParameters();
				--EditorGUI.indentLevel;
				EditorGUILayout.Separator();
			}

			if (EditorUtil.Foldout("Animator Controller Parameters", true))
			{
				++EditorGUI.indentLevel;
				if (uAnimator.animator != null)
				{ DrawAnimatorReorderableList(); }
				else
				{ EditorGUILayout.HelpBox("Animator is not available.", MessageType.Warning); }
				--EditorGUI.indentLevel;
				EditorGUILayout.Separator();
			}

			serializedObject.ApplyModifiedProperties();
		}

		protected void DrawAnimator()
		{
			var findFromChildren = EditorUtil.EditorOnlyToggle("Find From Children", "uLipSyncAnimator", true);
			EditorUtil.DrawProperty(serializedObject, nameof(findFromChildren));

			if (findFromChildren)
			{
				DrawAnimatorsInChildren();
			}
			else
			{
				if (uAnimator.animator == null)
				{
					EditorGUILayout.HelpBox("Animator is not assigned.", MessageType.Warning);
					EditorUtil.DrawProperty(serializedObject, nameof(uAnimator.animator));
				}
				else
				{
					EditorUtil.DrawProperty(serializedObject, nameof(uAnimator.animator));
				}
			}
		}

		protected void DrawAnimatorsInChildren()
		{
			var animators = uAnimator.GetComponentsInChildren<Animator>();
			if (animators.Length == 0)
			{
				EditorGUILayout.HelpBox("Animator is not found in children.", MessageType.Warning);
			}
			else
			{

				int index = 0;
				for (int i = 0; i < animators.Length; ++i)
				{
					var animator = animators[i];
					if (animator == uAnimator.animator)
					{
						index = i;
						break;
					}
				}
				var names = animators.Select(x => x.gameObject.name).ToArray();
				var newIndex = EditorGUILayout.Popup("Animators", index, names);
				if (newIndex != index)
				{
					Undo.RecordObject(target, "Change Animator");
					uAnimator.animator = animators[newIndex];
				}
			}
		}

		protected void DrawAnimatorReorderableList()
		{
			if (_reorderableList == null)
			{
				_reorderableList = new ReorderableList(uAnimator.parameters, typeof(MfccData));
				_reorderableList.drawHeaderCallback = rect =>
				{
					rect.xMin -= EditorGUI.indentLevel * 12f;
					EditorGUI.LabelField(rect, "Phoneme - Parameter Table");
				};
				_reorderableList.draggable = true;
				_reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
				{
					DrawParameterListItem(rect, index);
				};
				_reorderableList.elementHeightCallback = index =>
				{
					return GetParameterListItemHeight(index);
				};
			}

			EditorGUILayout.Separator();
			EditorGUILayout.BeginHorizontal();
			var indent = EditorGUI.indentLevel * 12f;
			EditorGUILayout.Space(indent, false);
			_reorderableList.DoLayoutList();
			EditorGUILayout.EndHorizontal();
		}

		protected void DrawParameterListItem(Rect rect, int index)
		{
			rect.y += 2f;
			rect.height = EditorGUIUtility.singleLineHeight;

			var par = uAnimator.animator.parameters;
			var uPar = uAnimator.parameters[index];
			float singleLineHeight =
				EditorGUIUtility.singleLineHeight +
				EditorGUIUtility.standardVerticalSpacing;

			uPar.phoneme = EditorGUI.TextField(rect, "Phoneme", uPar.phoneme);

			rect.y += singleLineHeight;

			var newIndex = EditorGUI.Popup(rect, "Parameter", uPar.index + 1, GetParameterArray());
			if (newIndex != uPar.index + 1 || uPar.name != par[uPar.index + 1].name)
			{
				Undo.RecordObject(target, "Change Parameter");
				uPar.index = newIndex - 1;
				uPar.name = par[uPar.index + 1].name;
				uPar.nameHash = Animator.StringToHash(uPar.name);
				Debug.Log($"parameter: {uPar.name} - {uPar.nameHash}");
			}

			rect.y += singleLineHeight;

			float weight = EditorGUI.Slider(rect, "Max Weight", uPar.maxWeight, 0f, 1f);
			if (weight != uPar.maxWeight)
			{
				Undo.RecordObject(target, "Change Max Weight");
				uPar.maxWeight = weight;
			}

			rect.y += singleLineHeight;
		}

		protected virtual float GetParameterListItemHeight(int index)
		{
			return 64f;
		}

		protected virtual string[] GetParameterArray()
		{
			if (uAnimator.animator == null)
			{
				return new string[0];
			}
			// get parameters from animator
			var parAnimator = uAnimator.animator.parameters;
			var names = new List<string>();
			for (int i = 0; i < parAnimator.Length; ++i)
			{
				var name = parAnimator[i].name;
				names.Add(name);
			}
			return names.ToArray();
		}

		protected void DrawParameters()
		{
			Undo.RecordObject(target, "Change Volume Min/Max");
			EditorGUILayout.MinMaxSlider(
				"Volume Min/Max (Log10)",
				ref uAnimator.minVolume,
				ref uAnimator.maxVolume,
				-5f, 0f);

			var rect = EditorGUILayout.GetControlRect(GUILayout.Height(0f));
			rect.x += EditorGUIUtility.labelWidth;
			rect.width -= EditorGUIUtility.labelWidth;
			rect.height = EditorGUIUtility.singleLineHeight;
			EditorGUILayout.BeginHorizontal();
			{
				var origColor = GUI.color;
				var style = new GUIStyle(GUI.skin.label);
				style.fontSize = 9;
				GUI.color = Color.gray;

				var minPos = rect;
				minPos.x -= 24f;
				minPos.y -= 12f;
				if (uAnimator.minVolume > -4.5f)
				{
					minPos.x += (uAnimator.minVolume + 5f) / 5f * rect.width - 30f;
				}
				EditorGUI.LabelField(minPos, $"{uAnimator.minVolume.ToString("F2")}", style);

				var maxPos = rect;
				var maxX = (uAnimator.maxVolume + 5f) / 5f * rect.width;
				maxPos.y -= 12f;
				if (maxX < maxPos.width - 48f)
				{
					maxPos.x += maxX;
				}
				else
				{
					maxPos.x += maxPos.width - 48f;
				}
				EditorGUI.LabelField(maxPos, $"{uAnimator.maxVolume.ToString("F2")}", style);
				GUI.color = origColor;
			}
			EditorGUILayout.EndHorizontal();

			EditorUtil.DrawProperty(serializedObject, nameof(uAnimator.smoothness));
		}
	}
}
