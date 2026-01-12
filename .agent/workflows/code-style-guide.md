---
trigger: model_decision
description: When in EXECUTION MODE for coding.
---

Task: Generate high-quality, production-ready code that adheres to strict professional standards. You must validate your own output through a recursive internal review process before displaying the final result.

1. NEGATIVE CONSTRAINTS (What NOT to do)
   NO Roleplay: Do not simulate characters, personas (e.g., "Architect," "Auditor"), or dialogue.

NO Conversational Comments: Do not include "thought process" comments in the code (e.g., // I added this to fix null, // Step 1: Loop).

NO Unverified Code: Do not output code without performing an impact analysis on potential breaking changes.

NO "Yes-Man" Responses: Do not blindly accept the user's logic if it violates SOLID principles or introduces security risks.

NO Markdown Fluff: Do not include conversational filler before or after the code (e.g., "Here is the code you asked for...").

2. CHAIN OF THOUGHT (Process)
   Perform the following reasoning steps explicitly before generating the final code:

Drafting: Mentally construct the solution using SOLID principles.

Impact Analysis: Simulate the integration of this code. Ask: "Does this break existing dependencies? Are edge cases (nulls, empty lists) handled?"

Refinement: Strip all non-standard comments. Ensure naming conventions are semantic.

Final Verification: Compare against the "Few-Shot" examples below to ensure compliance.

3. FEW-SHOT LEARNING (Examples)
   Input: "Create a function to divide two numbers."

❌ BAD Output (Violates Negative Constraints):

Python

# I am the Architect here is your code

def div(a, b): # Check if b is zero so we don't crash
if b == 0:
return None # Return nothing
return a / b
✅ GOOD Output (Adheres to Protocol):

Python

def divide_numbers(numerator: float, denominator: float) -> float:
"""
Calculates the quotient of two numbers.

    :param numerator: The dividend.
    :param denominator: The divisor.
    :raises ValueError: If the denominator is zero.
    :return: The result of the division.
    """
    if denominator == 0:
        raise ValueError("Denominator cannot be zero.")
    return numerator / denominator

4. REQUIRED OUTPUT FORMAT
   A. Impact Analysis & Logic

Risk Assessment: [Briefly describe potential risks or breaking changes analyzed]

Verification: [Confirm input validation and error handling strategies used]

B. Production Code

[The final code block, strictly following the Documentation Standards in the examples]
