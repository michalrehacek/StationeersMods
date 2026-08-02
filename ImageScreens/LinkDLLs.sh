#!/bin/bash

# Colors
RESET=$( echo -e "\e[0m" )
RED=$( echo -e "\e[0;31m" )
GREEN=$( echo -e "\e[0;32m" )
BLUE=$( echo -e "\e[0;34m" )

# Argument needs to be directory with Stationeers game
STATIONEERS="$1"
if [ "$STATIONEERS" == "" ]
then
	echo "${RED}Expecting argument - Stationeers install directory.${RESET}"
	exit 1
fi
if ! [ -f "$STATIONEERS/rocketstation.exe" ]
then
	echo "${BLUE}$STATIONEERS${RED} doesn't contain rocketstation.exe - it's not a directory with Stationeers installation.${RESET}"
	exit 1
fi
echo "Using ${BLUE}$STATIONEERS${RESET} as the Stationeers install directory."
echo

# Change to Assets/Assemblies
BASE="$( dirname "$0" )"
DIR="$BASE/Assets/Assemblies"
cd "$DIR"

# Find all *.dll.meta files...
EXISTED=0
SUCCESS=0
MISSING=0
for META in *.dll.meta
do
	# ...that don't have a DLL yet
	DLL=${META%.meta}
	if [ -f "$DLL" ]
	then
		echo "${GREEN}$DLL${RESET} already exists, skipping..."
		(( EXISTED++ ))
		continue
	fi

	# Try to find the DLL inside the Stationeers directory
	FOUND=$( find "$STATIONEERS" -name "$DLL" | head )
	if [[ "$FOUND" == "" ]]
	then
		echo "${RED}$DLL${RESET} not found inside the Stationeers directory, skipping..."
		(( MISSING++ ))
		continue
	fi

	# Copy the DLL into the Assets/Stationeers directory
	cp "$FOUND" "."
	echo "${GREEN}$DLL${RESET} copied from $FOUND..."
	(( SUCCESS++ ))
done
echo

# Summary
echo "Installed $SUCCESS files, $EXISTED already existed."
if (( MISSING > 0 ))
then
	echo "${RED}Couldn't find $MISSING files. The setup isn't complete.${RESET}"
fi

# EOF #
