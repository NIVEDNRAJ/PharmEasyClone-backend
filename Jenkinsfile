pipeline {
 
    agent any
 
    environment {
        IMAGE = "pharmeasy-api:${BUILD_NUMBER}"
        NETWORK = "pharmeasy-net"
        MYSQL_CONT = "pharmeasy-mysql"
        API_CONT = "pharmeasy-api"
 
        MYSQL_PWD = "root"
        MYSQL_DB = "pharm_easy_db"

        // Securely retrieve credentials from Jenkins Store (Secret Text types)
        BREVO_API_KEY = credentials('PHARMEASY_BREVO_API_KEY')
        JWT_SECRET = credentials('PHARMEASY_JWT_SECRET')
        RAZORPAY_KEY_ID = credentials('PHARMEASY_RAZORPAY_KEY_ID')
        RAZORPAY_KEY_SECRET = credentials('PHARMEASY_RAZORPAY_KEY_SECRET')
    }
 
    stages {
 
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Setup Environment File') {
            steps {
                bat """
                if not exist .env (
                    echo # Default Environment File > .env
                    echo MYSQL_CONNECTION_STRING="Server=pharmeasy-mysql;Port=3306;Database=pharm_easy_db;User=root;Password=root;" >> .env
                    echo JWT_ISSUER=PharmEasyCloneBackend >> .env
                    echo JWT_AUDIENCE=PharmEasyCloneAngular >> .env
                    echo JWT_SECRET=%JWT_SECRET% >> .env
                    echo JWT_ACCESS_TOKEN_MINUTES=60 >> .env
                    echo JWT_REFRESH_TOKEN_DAYS=30 >> .env
                    echo MEDIA_BASE_URL=/media >> .env
                    echo BREVO_API_KEY=%BREVO_API_KEY% >> .env
                    echo RAZORPAY_KEY_ID=%RAZORPAY_KEY_ID% >> .env
                    echo RAZORPAY_KEY_SECRET=%RAZORPAY_KEY_SECRET% >> .env
                    echo ASPNETCORE_ENVIRONMENT=Development >> .env
                    echo ASPNETCORE_URLS=http://+:8080 >> .env
                )
                """
            }
        }
 
        stage('Build Docker Image') {
            steps {
                bat "docker build -t %IMAGE% ."
            }
        }
 
        stage('Create Network') {
            steps {
                bat "docker network create %NETWORK% 2>nul || ver > nul"
            }
        }
 
        stage('Start MySQL') {
            steps {
                bat """
                docker rm -f %MYSQL_CONT% 2>nul || ver > nul
 
                docker run -d --name %MYSQL_CONT% --network %NETWORK% ^
                    -e MYSQL_ROOT_PASSWORD=%MYSQL_PWD% ^
                    -e MYSQL_DATABASE=%MYSQL_DB% ^
                    -p 3307:3306 ^
                    -v mysql-data:/var/lib/mysql ^
                    mysql:8.0
                """
            }
        }
 
        stage('Wait for MySQL (HEALTHCHECK equivalent)') {
            steps {
                bat """
                echo Waiting for MySQL to be ready...
 
                :loop
                docker exec %MYSQL_CONT% mysqladmin ping -h localhost -uroot -p%MYSQL_PWD% >nul 2>&1
 
                IF ERRORLEVEL 1 (
                    timeout /t 5 >nul
                    goto loop
                )
 
                echo MySQL is ready!
                """
            }
        }
 
        stage('Run API') {
            steps {
                bat """
                docker rm -f %API_CONT% 2>nul || ver > nul
 
                docker run -d --name %API_CONT% --network %NETWORK% --network-alias api ^
                    --env-file .env ^
                    -e MYSQL_CONNECTION_STRING="Server=pharmeasy-mysql;Port=3306;Database=pharm_easy_db;User=root;Password=root;" ^
                    -p 5095:8080 ^
                    -v api-media:/app/SimpleStorage ^
                    %IMAGE%
                """
            }
        }
 
    }
}