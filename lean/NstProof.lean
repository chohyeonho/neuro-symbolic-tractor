/-
Proofs about nst. Each rule of nst's constitution gets a model of the code that keeps it
and a theorem about that model.

Rule 1: one call of `nst lines` prints at most 120 lines of a file.
`selectLines` models `Lines.Select` in src/Nst.Domain/Lines.cs; keep the two in the same shape.
The model takes the file as its list of lines and leaves reading and printing out.
-/
namespace NstProof

def maxLines : Nat := 120

def selectLines (fileLines : List String) (start0 count : Nat) : List String :=
  (fileLines.drop start0).take (min count maxLines)

theorem selectLines_le_maxLines (fileLines : List String) (start0 count : Nat) :
    (selectLines fileLines start0 count).length ≤ maxLines := by
  unfold selectLines
  exact Nat.le_trans (List.length_take_le _ _) (Nat.min_le_right _ _)

/-- The rule's own number. The bound is written as 120 here, not as `maxLines`, so changing the
constant alone breaks this proof instead of moving the bound with it. -/
theorem selectLines_le_120 (fileLines : List String) (start0 count : Nat) :
    (selectLines fileLines start0 count).length ≤ 120 :=
  selectLines_le_maxLines fileLines start0 count

end NstProof
