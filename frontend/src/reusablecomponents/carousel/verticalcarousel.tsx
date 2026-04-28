import React, { useCallback, useEffect, useRef } from "react";
import type { EmblaOptionsType, EmblaCarouselType } from "embla-carousel";
import useEmblaCarousel from "embla-carousel-react";
import { PrevButton, NextButton, usePrevNextButtons } from "./carouselbuttons";
import "./carousel.css";

type PropType = {
    slides: React.ReactElement[];
    options?: EmblaOptionsType;
    currentSlideIndex: number;
    onIndexChange: (newIndex: number) => void;
};

const VerticalCarousel: React.FC<PropType> = ({
    slides,
    options,
    currentSlideIndex,
    onIndexChange,
}) => {
    const [emblaRef, emblaApi] = useEmblaCarousel(options);

    // Track the last index we wrote TO embla, so we can distinguish
    // "embla moved because the parent changed currentSlideIndex" (don't
    // re-emit) from "embla moved because the user dragged" (emit).
    const programmaticTargetRef = useRef<number | null>(null);

    // Sync external index changes INTO embla. When the parent dispatches a
    // new currentSlideIndex (e.g. song ended → onNext() → setIndex), we
    // scroll embla to match. We skip this when embla is already at the
    // target index, which happens when the change came FROM embla itself.
    useEffect(() => {
        if (!emblaApi) return;
        if (currentSlideIndex < 0) return;
        const emblaCurrent = emblaApi.selectedScrollSnap();
        if (emblaCurrent === currentSlideIndex) return;

        programmaticTargetRef.current = currentSlideIndex;
        // Animated transition (jump=false). The initial mount also goes
        // through this path; if you want the first scroll to be instant,
        // gate on a "first run" ref and pass jump=true for that one call.
        emblaApi.scrollTo(currentSlideIndex, false);
    }, [emblaApi, currentSlideIndex]);

    const onSelect = useCallback(
        (api: EmblaCarouselType) => {
            const newIndex = api.selectedScrollSnap();

            if (programmaticTargetRef.current === newIndex) {
                programmaticTargetRef.current = null;
                return;
            }

            if (newIndex !== currentSlideIndex) {
                onIndexChange(newIndex);
            }
        },
        [currentSlideIndex, onIndexChange]
    );

    useEffect(() => {
        if (!emblaApi) return;
        emblaApi.on("select", onSelect).on("reInit", onSelect);
        return () => {
            emblaApi.off("select", onSelect).off("reInit", onSelect);
        };
    }, [emblaApi, onSelect]);

    const {
        prevBtnDisabled,
        nextBtnDisabled,
        onPrevButtonClick,
        onNextButtonClick,
    } = usePrevNextButtons(emblaApi);

    return (
        <section className="embla">
            <div className="embla__viewport" ref={emblaRef}>
                <div className="embla__container">
                    {slides.map((Content, index) => (
                        <div className="embla__slide" key={index}>
                            {Content}
                        </div>
                    ))}
                </div>
            </div>

            <div className="embla__controls">
                <div className="embla__buttons">
                    <PrevButton onClick={onPrevButtonClick} disabled={prevBtnDisabled} />
                    <NextButton onClick={onNextButtonClick} disabled={nextBtnDisabled} />
                </div>
            </div>
        </section>
    );
};

export default VerticalCarousel;