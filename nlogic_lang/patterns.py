"""
Short program to create the grammar rules for the registers for the nlogic .tmLanguage.json file
"""

raw = """flag exe pc skip rtrn link compa compb compr iadn iadf rbase rofst rmem wbase wofst wmem gpa gpb gpc gpd gpe gpf gpg gph alum alua alub alur fpum fpua fpub fpur"""

regs = raw.split(" ")
for r in regs:
    pattern = f"(?i){r}"
    rule = '{{\n\t"match": "{0}",\n\t"name": "nlogic.{1}"\n}},'.format(pattern, r)
    print(rule)
