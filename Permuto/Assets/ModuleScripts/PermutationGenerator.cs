using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PermutationGenerator : MonoBehaviour {

	public List<List<int>> GetPermutations(List<int> list)
	{
		var result = new List<List<int>>();

		if (list.Count == 0)
		{
			result.Add(new List<int>());
			return result;
		}

		for (int i = 0; i < list.Count; i++)
		{
			// Take the element at index i
			int element = list[i];

			// Create a sublist excluding the element at index i
			List<int> remainingList = new List<int>(list);
			remainingList.RemoveAt(i);

			// Recursively get permutations of the remaining list
			var subPermutations = GetPermutations(remainingList);

			// Prepend the current element to each of the sublist permutations
			foreach (var sublist in subPermutations)
			{
				var newPerm = new List<int> { element };
				newPerm.AddRange(sublist);
				result.Add(newPerm);
			}
		}

		return result;
	}

    // Calculate feedback for a guess against the solution
    public int CalculateFeedback(List<int> solution, List<int> guess)
    {
        int correctPositions = 0;
        for (int i = 0; i < solution.Count; i++)
        {
            if (solution[i] == guess[i])
            {
                correctPositions++;
            }
        }
        return correctPositions;
    }

    // Generate guesses and feedback to solve the puzzle
    public List<KeyValuePair<List<int>, int>> GenerateGivenInformation(List<int> solution)
    {
        int n = solution.Count;
        var allPermutations = GetPermutations(solution).ToList();

        var remainingPossibilities = new List<List<int>>(allPermutations);
        var info = new List<KeyValuePair<List<int>, int>>();

        while (remainingPossibilities.Count > 1)
        {
			int tries = 0;

            var guess = remainingPossibilities[UnityEngine.Random.Range(0, remainingPossibilities.Count)];
            int feedback = CalculateFeedback(solution, guess);

			// try not to give a hint with more than 2 correct
			// but if we have to, eh okay
			while (feedback >= 3){
				guess = remainingPossibilities[UnityEngine.Random.Range(0, remainingPossibilities.Count)];
				feedback = CalculateFeedback(solution, guess);

				tries++;
				if(tries > 10000){
					break;
				}
			}
            info.Add(new KeyValuePair<List<int>, int>(guess, feedback));

            remainingPossibilities = remainingPossibilities
                .FindAll(perm => CalculateFeedback(perm, guess) == feedback);
        }

        return info;
    }

	public List<int> GenerateSolution(List<int> range)
	{
		List<List<int>> allPermutations = GetPermutations(range).ToList();

		// Select a random permutation
		if (allPermutations.Count > 0)
		{
			int randomIndex = UnityEngine.Random.Range(0, allPermutations.Count);
			return allPermutations[randomIndex];
		}
		// no permutations exist
		return new List<int>();
	}

	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
