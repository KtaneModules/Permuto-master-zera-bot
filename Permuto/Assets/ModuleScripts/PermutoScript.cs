using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;

public class PermutoScript : MonoBehaviour {

	// Variables
	[Header("Stuff")]
	public KMBombInfo bomb;
	public KMAudio audio;

	[Header("Objects")]
	public KMSelectable nextButton;
	public KMSelectable submitButton;
	public PermutationGenerator generator;
	public TextMesh submitButtonText;
	public TextMesh displayText;
	public KMSoundOverride.SoundEffect clickSound;
	[Header("Game Variables")]
	//Logging
	static int moduleIdCounter = 1;
	int moduleId;
	bool solved = false;

	// game stuff
	int stage = 1;
	public List<int> correctState;
	// stage 1
	int currentQuery = 0;
	
	public List<KeyValuePair<List<int>, int>> givenInformation;
	// stage 2
	List<int> currentPermutation = new List<int>{0,1,2,3,4};


	public Material[] colors; // the color materials
	public string[] colorStrings;
	public MeshRenderer[] buttonRenderers;
	public KMSelectable[] buttons;

	// misc necessary funtions

	string IntListToColorString(List<int> state){
		string str = "";
		for (int i = 0; i<state.Count; i++){
			str += colorStrings[state[i]];
			if (i < state.Count-1){
				str += ", ";
			}
		}
		return str;
	}

	// functions for color swapping

	int selectedIndex = -1;
	void Swap(int indexA, int indexB){
		// swap the two indices in currentPermutation
		int temp = currentPermutation[indexA];
		currentPermutation[indexA] = currentPermutation[indexB];
		currentPermutation[indexB] = temp;
		RenderStage2();
	}

	void SelectColor(int index){
		if (stage == 2){
			if (selectedIndex == -1){
				selectedIndex = index;
				// TODO add additional highlight
			} else if (index == selectedIndex){ // de-select item
				selectedIndex = -1;
			} else { // already selected first, swap two items
				Swap(selectedIndex, index);
				selectedIndex = -1;
			}
		}
		buttons[index].AddInteractionPunch(0.75f);
		audio.PlayGameSoundAtTransform(clickSound,buttons[index].transform);
	}

	// Rendering the stages

	void RenderStage1(int queryNumber){
		displayText.text = givenInformation[queryNumber].Value.ToString();
		for (int i = 0; i<5; i++){
			Material color = colors[givenInformation[queryNumber].Key[i]];
			buttonRenderers[i].material = color;
		}
		// disable highlights
	}

	void RenderStage2(){ // render current state
		for (int i = 0; i<5; i++){
			Material color = colors[currentPermutation[i]];
			buttonRenderers[i].material = color;
		}
		// enable highlights
	}

	// Buttons

	void PressNextButton(){
		if (stage == 2){
			stage = 1;
			currentQuery = -1; // will be set to 0 shortly after
			// TODO disable buttons & highlights
			submitButtonText.text = "READY";
		}

		currentQuery = (currentQuery+1)%givenInformation.Count;
		RenderStage1(currentQuery);

		nextButton.AddInteractionPunch(0.75f);
		audio.PlayGameSoundAtTransform(clickSound,nextButton.transform);
	}

	void PressSubmitButton(){
		if (stage == 1){ // Stage 1 --> Stage 2
			stage = 2;
			RenderStage2();
			// TODO enable buttons & highlights
			submitButtonText.text = "SUBMIT";
			displayText.text = "";
		} else if (!solved) { // submission
			int total = 0;
			for (int i = 0; i < correctState.Count; i++){
				if (currentPermutation[i] == correctState[i]){
					total++;
				}
			}

			if (total == correctState.Count){
				// disarm
				solved = true;
				GetComponent<KMBombModule>().HandlePass();

				Debug.LogFormat("[Permuto #{0}] Correct permutation {1} submitted; module disarmed.", moduleId, IntListToColorString(currentPermutation));
			} else {
				// strike
				Debug.LogFormat("[Permuto #{0}] Incorrect permutation {1} submitted; module gave a strike.", moduleId, IntListToColorString(currentPermutation));
				GetComponent<KMBombModule>().HandleStrike();
			}
		}

		submitButton.AddInteractionPunch(0.75f);
		audio.PlayGameSoundAtTransform(clickSound,submitButton.transform);
	}

	// Initialization stuff

	void Awake(){
		moduleId = moduleIdCounter++;
		for (int i = 0; i < buttons.Length; i++){
			// i love how all languages make me do this :grr:
			int new_i = i;
			KMSelectable bt = buttons[new_i];
			bt.OnInteract += delegate(){SelectColor(new_i); return false;};
		}
		nextButton.OnInteract += delegate(){PressNextButton(); return false;};
		submitButton.OnInteract += delegate(){PressSubmitButton(); return false;};
	}

	void Start () {
		correctState = generator.GenerateSolution(currentPermutation);
		givenInformation = generator.GenerateGivenInformation(correctState);
		RenderStage1(currentQuery);

		// debug stuff

		List<string> detailedGivenInformation = new List<string>();
		foreach (KeyValuePair<List<int>,int> pair in givenInformation){
			string str = IntListToColorString(pair.Key);
			str += " → " + pair.Value.ToString();
			detailedGivenInformation.Add(str);
		}

		Debug.LogFormat("[Permuto #{0}] Correct Permutation is {1}.", moduleId, IntListToColorString(correctState));
		for (int i = 0; i<detailedGivenInformation.Count; i++){
			Debug.LogFormat("[Permuto #{0}] Clue #{1}: {2}",moduleId,(i+1).ToString(),detailedGivenInformation[i]);
		}
	}
	
	// Twitch plays
	#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"Use !next to press the NEXT button, use !ready or !submit to press the submit button, and use !swap X Y to swap the colors at indices X and Y (starting with 1). If you have multiple pairs you may do !swap X Y Y Z etc.";
	#pragma warning restore 414

	
	IEnumerator ProcessTwitchCommand(string command){
		command = command.ToLowerInvariant();
		Debug.LogFormat("[Permuto #{0}] Ran Twitch command: {1}",moduleId.ToString(),command);

		var match = Regex.Match(command, @"^\s*(?:next)\s*", RegexOptions.IgnoreCase);
		if (match.Success){ // next button
			PressNextButton();
		}

		match = Regex.Match(command, @"^\s*(?:ready)\s*", RegexOptions.IgnoreCase);
		if (match.Success && stage == 1){
			PressSubmitButton();
		}

		match = Regex.Match(command, @"^\s*(?:submit)\s*", RegexOptions.IgnoreCase);
		if (match.Success && stage == 2){
			PressSubmitButton();
		}

		match = Regex.Match(command, @"^\s*(?:swap)\s*", RegexOptions.IgnoreCase);
		if (match.Success && stage == 2){
			command = command.Replace(" ","");
			command = command.Replace("swap", "");

			// swap pairs
			string[] splitString = command.Select(c => c.ToString()).ToArray();
			if (splitString.Length > 1){
				for (int i=0; i<splitString.Length; i+=2){ // for each pair
					int firstInd;
					int secondInd;
					bool firstIsNumber = int.TryParse(splitString[i], out firstInd);
					bool secondIsNumber = int.TryParse(splitString[i+1], out secondInd);
					if(firstIsNumber && secondIsNumber){
						bool firstWithinRange = firstInd>=1 && firstInd<=currentPermutation.Count;
						bool secondWithinRange = firstInd>=1 && firstInd<=currentPermutation.Count;
						if (firstWithinRange && secondWithinRange){
							Swap(firstInd-1, secondInd-1);
							yield return new WaitForSeconds(0.05f);
						}
					}
				}
			}
		}

		yield return null;
	}	

}
