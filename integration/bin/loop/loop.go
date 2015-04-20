package main

import (
	"fmt"
	"time"
)

func main() {
	for {
		time.Sleep(100 * time.Millisecond)
		fmt.Println("I'm alive")
	}
}
