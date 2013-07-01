#include <Servo.h>

Servo lservo;
Servo rservo;

int pos = 0;

/*int main(void)
{
  init();
  setup();

//  for(pos = 0; pos < 180; pos += 1)  // goes from 0 degrees to 180 degrees 
//  {                                  // in steps of 1 degree 
//    myServo.write(180-pos);              // tell servo to go to position in variable 'pos' 
//    myServoTest.write(pos);
//    delay(15);                       // waits 15ms for the servo to reach the position 
//  }
  //This is where your program goes.  The default main() just calls setup then
  //continuously call loop().
  loop();
  
  return 0;
}*/

void setup(){
  Serial.begin(9600);
  
  lservo.attach(9);
  rservo.attach(10);
  lservo.write(90);
  rservo.write(90);
  
//  Serial.println("nice??");
//  
//  for(pos = 0; pos < 180; pos += 1)  // goes from 0 degrees to 180 degrees 
//  {                                  // in steps of 1 degree 
//    myServo.write(180-pos);              // tell servo to go to position in variable 'pos' 
//    myServoTest.write(pos);
//    delay(15);                       // waits 15ms for the servo to reach the position 
//    Serial.println(pos);
//  }
}

unsigned char x, y, z, id = 0;
int val = 0;

void loop(){
  
  if (Serial.available() >= 4) {
    x = Serial.read();
    y = Serial.read();
    z = Serial.read();
    id = Serial.read();
    
    if (id == 7) {
      lservo.write(180 - y);
      delay(10);
    } 
  
    if (id == 11) {
      rservo.write(y);
      delay(10); 
    }
  }
  
  //  if(pos < 100){
  //    myServo.write(850);
  //    myServoTest.write(850);
  //    pos += 1;
  //  }
  //  for(pos = 0; pos < 180; pos += 1)  // goes from 0 degrees to 180 degrees 
  //  {                                  // in steps of 1 degree 
  //    myServo.write(180-pos);              // tell servo to go to position in variable 'pos' 
  //    myServoTest.write(pos);
  //    delay(15);                       // waits 15ms for the servo to reach the position 
  //  } 
  //  for(pos = 180; pos>=1; pos-=1)     // goes from 180 degrees to 0 degrees 
  //  {                                
  //    myServo.write(pos);              // tell servo to go to position in variable 'pos' 
  //    myServoTest.write(180-pos);
  //    delay(15);                       // waits 15ms for the servo to reach the position 
  //  } 
}










