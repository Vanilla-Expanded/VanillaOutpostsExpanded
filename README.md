# VanillaOutpostsExpanded
Proposed fixes:  

Outpost_Encampment.cs:
Removed ";" from the end of the namespace and added top level {} that Visual Studio balked at when trying to build.

Outpost_Scavenging.cs:
Current behavior is the outpost will ONLY deliver TechPrints when they produce.  This is due to the Generate() call not being given any parameters, update passes a market value of 750-2000 to the call.  This causes the outpost to produce random expected rewards.  I tried a couple of values for market ranges and 750-2000 felt good but feel free to modify if desired.  *Perhaps a formula that derives the reward range by colony wealth so that it scales would be nice but I defer to the teams judgment

Outposts.xml and Outpost_Drilling.cs:
The original bug was that the drilling outpost would not produce Chemfuel, you would get the letter on production but it would be void of chemfuel and would not be stored\delivered, this appeared to be a simple reversing of the logic.  When Ready was true it would create an empty list rather than producing, reversing these fixed the delivery issue.  

While working on this I also noticed a disconnect between the description of the drilling outpost infographic in the mod description vs its actual behavior.  The description states it takes 7 days (modifiable in the mod options) to drill before the outpost starts producing and it also mentions that production is based off of mining skill (rate of 500/10 mining skill).  The issue with the drill time was that the 7 days description was based off a construction skill of 1, but the outpost requires 20 to establish this caused the drilling time to be ~8 hours (essentially negligible).  This update multiplies the WorktoDrill values by 20 to make a minimally skilled outpost actually take 7 days to complete (and can scale down with higher skill). The Def for the drilling outpost was actually a flat rate not based on mining skill so I tweaked the XML to produce 50 chemfuel per mining skill, this causes the outpost to produce at the rate of the description.  With these tweaks the drilling outpost now works like the infographic depicts (yea no updating images?).  

Certainly look over these changes to ensure they are proper, in Rimworld terms my skill in C# is firmly in the “Utter Beginner” range.  Most of these edits were the outcome of (likely an embarrassingly large amount) Googling and ChatGPT to get me in the right direction.  
