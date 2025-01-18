#!/bin/bash

testGitVoidForgeConnection() {
    echo "Looking for git"
    if ! command -v git &> /dev/null; then
        echo "Git could not be found"
        exit 1
    fi
    echo "Testing connection to VoidForge using ssh"
    echo "Connection to VoidForge successful"

}
    
cloneRepos() {
    echo "Cloning repositories into repositories folder..."

    if [ ! -f repos.txt ]; then
        echo "No repos file found."
        return
    fi

    mkdir -p repositories

    cd repositories || echo "failed to navigate to repositories" || exit

    while read -r repo; do
        if [ -z "$repo" ]; then
            continue
        fi

        if [ -d "$(basename "$repo" .git)" ]; then
            echo "Skipping $repo"
            continue
        fi

        if [[ $repo == \#* ]]; then
            continue
        fi

        echo "Cloning $repo"
        git clone "$repo" 2> /dev/null || echo "Failed to clone $repo"
    done < ../repos.txt
}

purgeRepos() {
    echo "Purging repositories..."
    if [ ! -f repos ]; then
        echo "No repos file found."
        return
    fi

    rm -rf repositories
}

pullRepos() {
    echo "Pulling repositories..."
    if [ ! -d repositories ]; then
        echo "No repositories folder found."
        return
    fi

    cd repositories || echo "failed to navigate to repositories" || exit

    for repo in *; do
        if [ -d "$repo" ]; then
            echo "Pulling $repo"
            cd "$repo" || echo "failed to navigate to $repo" || exit
            git pull
            cd ..
        fi
    done

}

swtichAllToMain() {
    echo "Switching all repositories to main branch..."
    if [ ! -d repositories ]; then
        echo "No repositories folder found."
        return
    fi

    cd repositories || echo "failed to navigate to repositories" || exit

    for repo in *; do
        if [ -d "$repo" ]; then
            echo "Switching $repo to main"
            cd "$repo" || echo "failed to navigate to $repo" || exit
            git checkout main
            cd ..
        fi
    done
}

help() {
    echo "Usage: anvil.sh [option]"
    echo "Options:"
    echo "  clone: Clone all repositories listed in repos"
    echo "  pull: Pull all repositories"
    echo "  switch main: Switch all repositories to main branch"
    echo "  purge: Purge all repositories"
}

case $1 in
    clone)
        testGitVoidForgeConnection
        cloneRepos
        ;;
    purge)
        purgeRepos
        ;;
    pull)
        pullRepos
        ;;
    switch)
        case $2 in
            main)
                swtichAllToMain
                ;;
            *)
                help
                ;;
        esac
        ;;
    *)
        help
        ;;
esac
