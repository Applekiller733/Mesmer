import React, { Component, useEffect } from 'react'
import type { EmblaOptionsType } from 'embla-carousel'
import {
  PrevButton,
  NextButton,
  usePrevNextButtons
} from './carouselbuttons'
import useEmblaCarousel from 'embla-carousel-react'
import "./carousel.css"

type PropType = {
  slides: React.ReactElement[]
  options?: EmblaOptionsType
  onNext?: any
  onPrevious?: any
  currentSlideIndex?: any
}

const VerticalCarousel: React.FC<PropType> = (props) => {
  const { slides, options, onNext, onPrevious, currentSlideIndex } = props
  const [emblaRef, emblaApi] = useEmblaCarousel(options)

  useEffect(() => {
    if (!emblaApi) return;
    
    if (currentSlideIndex >= 0) {
      emblaApi.scrollTo(currentSlideIndex, true);
    }
  }, [emblaApi, slides, currentSlideIndex]);

  const {
    prevBtnDisabled,
    nextBtnDisabled,
    onPrevButtonClick,
    onNextButtonClick,
  } = usePrevNextButtons(emblaApi)

  function handleNext() {
    onNextButtonClick();
    if (onNext) onNext();
  }

  function handlePrevious() {
    onPrevButtonClick();
    if (onPrevious) onPrevious();
  }

  return (
    <section className="embla">
      <div className="embla__viewport" ref={emblaRef}>
        <div className="embla__container">
          {slides.map((Content, index) => (
            <div key={index}>
              {Content}
            </div>
          ))}
        </div>
      </div>

      <div className="embla__controls">
        <div className="embla__buttons">
          <PrevButton onClick={handlePrevious} disabled={prevBtnDisabled} />
          <NextButton onClick={handleNext} disabled={nextBtnDisabled} />
        </div>
      </div>
    </section>
  )
}

export default VerticalCarousel
