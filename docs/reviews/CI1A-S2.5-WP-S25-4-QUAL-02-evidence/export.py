import pathlib, subprocess, tarfile
root = pathlib.Path(__file__).resolve().parent
for profile, revision in {
    "baseline": "48370711a6247b972dfc83628618924e864be374",
    "corrected": "d48feeaa316f76901b6e37264d6d115f689a7000",
}.items():
    archive = root / (profile + ".tar")
    subprocess.run(["git", "--git-dir=D:/projects/Andirit/.git", "archive",
                    "--format=tar", f"--output={archive}", revision], check=True)
    destination = root / profile
    destination.mkdir(exist_ok=False)
    with tarfile.open(archive) as stream:
        stream.extractall(destination, filter="data")
    print(profile, revision, destination)
