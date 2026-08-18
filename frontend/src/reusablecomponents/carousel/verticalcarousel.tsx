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

    const programmaticTargetRef = useRef<number | null>(null);

    useEffect(() => {
        if (!emblaApi) return;
        if (currentSlideIndex < 0) return;
        const emblaCurrent = emblaApi.selectedScrollSnap();
        if (emblaCurrent === currentSlideIndex) return;

        programmaticTargetRef.current = currentSlideIndex;

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