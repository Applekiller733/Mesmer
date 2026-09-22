# SAM custom build for the online recommendation API (zip Lambda).
# Metadata.BuildMethod: makefile in template.yaml makes SAM invoke
# `make build-RecommendationApiFunction` with the working directory set to
# CodeUri (this folder) and $(ARTIFACTS_DIR) pointing at where the packaged
# function must land.
#
# Why a custom build instead of SAM's managed python builder: the managed
# builder installs the `requirements.txt` found in CodeUri, which here is the
# FULL set (librosa/scipy/scikit-learn/numba) used by the batch container — far
# over the 250 MB zip limit and unnecessary for serving. This target installs
# only the slim serving deps (requirements-api.txt) and copies just the three
# modules the API imports.
build-RecommendationApiFunction:
	python -m pip install -r requirements-api.txt -t "$(ARTIFACTS_DIR)"
	cp api.py db.py recommendation_logic.py "$(ARTIFACTS_DIR)"
