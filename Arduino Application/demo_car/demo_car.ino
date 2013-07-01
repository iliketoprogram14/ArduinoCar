#include <Servo.h>

Servo lservo;
Servo rservo;

unsigned char spd, id = 0;
int val = 0;
char l_offset = 4;
char r_offset = 5;

/* KEY
 *       Forward Backwards Still
 * Left    180      0       95
 * Right    0      180      95
 */

void setup(){
  Serial.begin(9600);
  
  lservo.attach(9);
  rservo.attach(10);
  lservo.write(90 + l_offset);
  rservo.write(90 + r_offset);
}

void loop(){
  
  if (Serial.available() >= 2) {
    spd = Serial.read();
    id = Serial.read();
    
    if (id == 7) {
      lservo.write(spd + l_offset);
    } 
  
    if (id == 11) {
      rservo.write(spd + r_offset);
    }
  }
}










