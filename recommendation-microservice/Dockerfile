# Container image for the scheduled background pipeline (batch_handler.handler).
# The online API ships separately as a small zip Lambda; this image carries the
# heavy native deps (librosa/scipy/scikit-learn/numba) that can't fit a zip.
#
# The pinned wheels are self-contained: psycopg2-binary bundles libpq, soundfile
# bundles libsndfile (>=1.1, so mp3/flac/ogg/wav decode without system packages),
# and numba/llvmlite bundle LLVM. So no OS package installs are needed. If some
# exotic audio format ever fails to decode, add ffmpeg for audioread's fallback.
#
# Base = the 3.12 image, which is Amazon Linux 2023 (glibc 2.34). The 3.11 image
# is Amazon Linux 2 (glibc 2.26), too old for the modern manylinux_2_28 wheels
# that librosa deps like soxr now ship — pip would fall back to compiling them
# from source, which fails (no compiler in the base image). AL2023 uses those
# wheels directly, so nothing builds from source.
FROM public.ecr.aws/lambda/python:3.12

# Install Python deps first so the layer caches across code-only changes.
COPY requirements.txt ${LAMBDA_TASK_ROOT}/
RUN pip install --no-cache-dir -r ${LAMBDA_TASK_ROOT}/requirements.txt

# Application code + pre-fit model artifacts (scaler/pca .pkl, read at transform
# time). The offline analysis scripts (*.pdf/*.png/*.csv) are excluded via
# .dockerignore to keep the image slim.
COPY *.py ${LAMBDA_TASK_ROOT}/
COPY *.pkl ${LAMBDA_TASK_ROOT}/

# librosa/numba/matplotlib need a writable cache/config dir; on Lambda the only
# writable location is /tmp (EphemeralStorage). Point every cache there.
ENV NUMBA_CACHE_DIR=/tmp \
    MPLCONFIGDIR=/tmp \
    XDG_CACHE_HOME=/tmp \
    HOME=/tmp

# The batch function; SAM sets ImageConfig.Command, but default to the same.
CMD ["batch_handler.handler"]
