from collections import defaultdict

def annotate_debug_file(debug_file_path, coverage_log_file_path):
    with open(debug_file_path, "r") as debug_file:
        debug_lines = debug_file.readlines()
    with open(coverage_log_file_path, "r") as coverage_file:
        coverage_lines = coverage_file.readlines()

    # build map of process > line > covered
    coverage_map = defaultdict(dict)
    for l in coverage_lines:
        (mmu, appdba, pc) = l.split("\t")
        if "Off" in mmu:
            continue
        process_coverage_map = coverage_map[appdba]
        process_coverage_map[pc.strip()] = True

    annotated_lines = []
    for l in debug_lines:
        try:
            (pc, code) = l.split(" |\t")
        except ValueError:
            print("Error")
        pc = pc.strip()
        if pc in coverage_map["APPDBA 00 00 00 01"]:
            annotation = "X"
        else:
            annotation = " "
        annotated_lines.append(f"{annotation} | {l}")
    return "".join(annotated_lines)

annotated_contents = annotate_debug_file(
    "nlogic_sim/OS/BUILD/DISK_DEBUG/66/1_handler.pro.debug",
    "nlogic_sim/OS/COVERAGE.txt"
)
with open("coverage_report.pro.debug", "w") as output_file:
    output_file.write(annotated_contents)
