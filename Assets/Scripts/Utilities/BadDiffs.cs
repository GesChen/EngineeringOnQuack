using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BadDiffs : MonoBehaviour {

	public enum State {
		Unset,
		Normal,
		Modified,
		Addition,
		Removal
	}

	public struct Item {
		public ulong c;
		public State state;
	}

	public class Diffs {
		public Item[] AItems;
		public Item[] BItems;
	}

	public static Diffs CheckDiffs(string[] A, string[] B) {
		ulong[] Alongs = A.Select(s => HF.Fnv1aHash64(s)).ToArray();
		ulong[] Blongs = B.Select(s => HF.Fnv1aHash64(s)).ToArray();

		return CheckDiffs(Alongs, Blongs);
	}

	public static Diffs CheckDiffs(ulong[] Alongs, ulong[] Blongs) {
		Item[] A = Alongs.Select(u => new Item() { c = u, state = State.Unset }).ToArray();
		Item[] B = Blongs.Select(u => new Item() { c = u, state = State.Unset }).ToArray();

		int Arems = 0;
		int Badds = 0;
		List<Item> shifted = new(B);

		int i = 0;
		for (; i < A.Length; i++) {
			Item cur = A[i];
			// try to find it
			int mi = i - Arems;

			int index = -1;
			for (int j = mi; j < shifted.Count; j++) {
				if (shifted[j].c == cur.c) {
					index = j;
					break;
				}
			}

			if (index == mi) {
				A[i].state = State.Normal;
				B[mi + Badds].state = State.Normal;
			} else
			if (index > mi) {
				// additions in B
				int prerem = Badds;

				for (int a = i; a < index; a++) {
					B[a + prerem].state = State.Addition;

					shifted.RemoveAt(i);

					Badds++;
				}

				A[i].state = State.Normal;
				B[mi + Badds].state = State.Normal;
			} else
			if (index == -1) {
				// no find to the right (in shifted)
				// look left and mark precedings as deleted if found lefter

				bool found = false;
				int lookleft = mi;
				int check = 0;

				if (mi < B.Length) {
					while (lookleft >= 0) {
						lookleft--;
						check++;

						if (B[lookleft + Badds].state == State.Normal)
							break;

						if (shifted[lookleft].c == cur.c) {
							found = true;
							break;
						}
					}
				}

				if (found) {
					for (int s = 0; s < check; s++) {
						A[i - s - 1].state = State.Removal;
						Arems += 1;
					}

					B[lookleft + Badds].state = State.Normal;
					A[i].state = State.Normal;

				} else {
					if (mi + Badds >= B.Length) {
						// end of B, all following in A is removed
						for (int r = i; r < A.Length; r++)
							A[r].state = State.Removal;

						break;
					} else {
						//ok
						A[i].state = State.Modified;
						B[mi + Badds].state = State.Modified;
					}
				}
			}
		}

		int endI = i - Arems + Badds + 1;
		if (endI < B.Length) {
			for (int f = endI; f < B.Length; f++)
				B[f].state = State.Addition;
		}

		return new() {
			AItems = A,
			BItems = B
		};
	}
}