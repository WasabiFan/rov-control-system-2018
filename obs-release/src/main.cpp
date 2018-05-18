#include <Audio.h>
//#include <Wire.h>
//#include <SPI.h>

AudioInputAnalog audioIn(A2);

AudioAnalyzeFFT1024 fft;
AudioConnection patchCord1(audioIn, fft);

#define NO_TIME (UINT32_MAX)
#define TONE_TIME_THRESH 1000
#define RESET_TIME_THRESH 500

int detectionStage = 0;
const int frequencyBuckets[] PROGMEM = {19, 23};
uint32_t lastStageEndTime = NO_TIME;
uint32_t detectionStartTime = NO_TIME;

void setup()
{
    Serial.begin(115200);
}

void activateTrigger()
{
    Serial.println("TRIGGER");
}

void loop()
{
    //adc1.update();
    //print1.update();
    //val = analogRead(A2);
    //Serial.println(val);
    //delay(5);

    if (fft.available())
    {
        uint32_t time = millis();
        bool isFreqActive = fft.read(frequencyBuckets[detectionStage]) > 0.05;
        if (isFreqActive)
        {
            if (detectionStartTime == NO_TIME)
            {
                detectionStartTime = time;
            }
            else if (time - detectionStartTime > TONE_TIME_THRESH)
            {
                Serial.print("Accepted stage ");
                Serial.print(detectionStage);
                Serial.println("; advancing");

                detectionStage++;
                lastStageEndTime = time;
                detectionStartTime = NO_TIME;

                if (detectionStage >= sizeof(frequencyBuckets) / sizeof(int))
                {
                    activateTrigger();
                    detectionStage = 0;
                    lastStageEndTime = NO_TIME;
                }
            }
        }
        else
        {
            detectionStartTime = NO_TIME;
            if (lastStageEndTime != NO_TIME && time - lastStageEndTime > RESET_TIME_THRESH)
            {
                Serial.print("Timed out waiting for stage ");
                Serial.println(detectionStage);
                lastStageEndTime = NO_TIME;
                detectionStage = 0;
            }
        }
    }
/*
    float n;
    int i;
    if (fft.available())
    {
        // each time new FFT data is available
        // print it all to the Arduino Serial Monitor
        Serial.print("FFT: ");
        for (i = 0; i < 40; i++)
        {
            n = fft.read(i);
            if (n >= 0.01)
            {
                Serial.print(n);
                Serial.print(" ");
            }
            else
            {
                Serial.print("   "); // don't print "0.00"
            }
        }
        Serial.println();
    }*/
}
