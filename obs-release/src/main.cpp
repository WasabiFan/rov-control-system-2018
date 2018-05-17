#include <Audio.h>
//#include <Wire.h>
//#include <SPI.h>

AudioInputAnalog audioIn(A2);

//AudioAnalyzeFFT1024 fft;
//AudioConnection patchCord1(audioIn, fft);
AudioAnalyzeToneDetect toneDetect;
AudioConnection patchCord1(audioIn, toneDetect);


void setup()
{                
  Serial.begin(115200);
  while(!Serial);
  Serial.println("DSFSF");
    AudioMemory(12);
    toneDetect.frequency(800, 100);
  //fft.windowFunction(AudioWindowHanning1024);
}

int val;

void loop()
{
  //adc1.update();
  //print1.update();
  //val = analogRead(A2);
  //Serial.println(val);
  //delay(5);

  /*float n;
  int i;
  if (fft.available()) {
    // each time new FFT data is available
    // print it all to the Arduino Serial Monitor
    Serial.print("FFT: ");
    for (i=0; i<40; i++) {
      n = fft.read(i);
      if (n >= 0.01) {
        Serial.print(n);
        Serial.print(" ");
      } else {
        Serial.print("   "); // don't print "0.00"
      }
    }
    Serial.println();
  }*/
  Serial.println(toneDetect.read());
  delay(10);
}
